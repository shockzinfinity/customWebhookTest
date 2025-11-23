using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Webhooks.Api.Data;
using Webhooks.Api.Models;
using Webhooks.Api.OpenTelemetry;

namespace Webhooks.Api.Services;

internal sealed class WebhookDispatcher(Channel<WebhookDispatch> webhooksChannel, IHttpClientFactory httpClientFactory, WebhooksDbContext dbContext)
{
    public async Task DispatchAsync<T>(string eventType, T data)
        where T : notnull
    {
        using Activity? activity = DiagnosticConfig.Source.StartActivity($"{eventType} dispatch webhook");
        activity?.AddTag("event.type", eventType);

        await webhooksChannel.Writer.WriteAsync(new WebhookDispatch(eventType, data, activity?.Id));
    }

    public async Task ProcessAsync<T>(string eventType, T data)
    {
        var subscriptions = await dbContext.WebhookSubscriptions.AsNoTracking().Where(s => s.EventType == eventType).ToListAsync();

        foreach (WebhookSubscription subscription in subscriptions)
        {
            using var httpClient = httpClientFactory.CreateClient();

            var payload = new WebhookPayload<T>
            {
                Id = Guid.NewGuid(),
                EventType = subscription.EventType,
                SubscriptionId = subscription.Id,
                Timestamp = DateTime.UtcNow,
                Data = data
            };
            string jsonPayload = JsonSerializer.Serialize(payload);

            try
            {
                var response = await httpClient.PostAsJsonAsync(subscription.WebhookUrl, payload);

                var attempt = new WebhookDeliverAttempt
                {
                    Id = Guid.NewGuid(),
                    WebhookSubscriptionId = subscription.Id,
                    Payload = jsonPayload,
                    ResponseStatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode,
                    Timestamp = DateTime.UtcNow,
                };
                dbContext.WebhookDeliverAttempts.Add(attempt);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var attempt = new WebhookDeliverAttempt
                {
                    Id = Guid.NewGuid(),
                    WebhookSubscriptionId = subscription.Id,
                    Payload = jsonPayload,
                    ResponseStatusCode = null,
                    Success = false,
                    Timestamp = DateTime.UtcNow,
                };
                dbContext.WebhookDeliverAttempts.Add(attempt);
                await dbContext.SaveChangesAsync();

                // Optionally, log the exception or handle it (not rethrowing to not disrupt other webhooks)
                Console.WriteLine($"Failed to send webhook to {subscription.WebhookUrl}:{ex.Message}");
            }
        }
    }
}