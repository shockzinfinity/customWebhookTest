using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Webhooks.Api.Data;
using Webhooks.Api.Models;

namespace Webhooks.Api.Services;

internal sealed class WebhookDispatcher
{
    private readonly WebhooksDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public WebhookDispatcher(WebhooksDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    public async Task DispatchAsync<T>(string eventType, T data)
    {
        var subscriptions = await _db.WebhookSubscriptions.AsNoTracking().Where(s => s.EventType == eventType).ToListAsync();

        foreach (WebhookSubscription subscription in subscriptions)
        {
            using var httpClient = _httpClientFactory.CreateClient();

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
                _db.WebhookDeliverAttempts.Add(attempt);
                await _db.SaveChangesAsync();
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
                _db.WebhookDeliverAttempts.Add(attempt);
                await _db.SaveChangesAsync();

                // Optionally, log the exception or handle it (not rethrowing to not disrupt other webhooks)
                Console.WriteLine($"Failed to send webhook to {subscription.WebhookUrl}:{ex.Message}");
            }
        }
    }
}