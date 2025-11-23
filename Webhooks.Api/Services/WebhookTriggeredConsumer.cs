using MassTransit;
using System.Text.Json;
using System.Text.Json.Nodes;
using Webhooks.Api.Data;
using Webhooks.Api.Models;

namespace Webhooks.Api.Services;

internal sealed class WebhookTriggeredConsumer(IHttpClientFactory httpClientFactory, WebhooksDbContext dbContext) : IConsumer<WebhookTriggered>
{
    public async Task Consume(ConsumeContext<WebhookTriggered> context)
    {
        using var httpClient = httpClientFactory.CreateClient();

        var payload = new WebhookPayload
        {
            Id = Guid.NewGuid(),
            EventType = context.Message.EventType,
            SubscriptionId = context.Message.SubscriptionId,
            Timestamp = DateTime.UtcNow,
            Data = context.Message.Data
        };
        var jsonPayload = JsonSerializer.Serialize(payload);

        try
        {
            var response = await httpClient.PostAsJsonAsync(context.Message.WebhookUrl, payload);
            response.EnsureSuccessStatusCode();

            var attempt = new WebhookDeliveryAttempt
            {
                Id = Guid.NewGuid(),
                WebhookSubscriptionId = context.Message.SubscriptionId,
                Payload = jsonPayload,
                ResponseStatusCode = (int)response.StatusCode,
                Success = response.IsSuccessStatusCode,
                Timestamp = DateTime.UtcNow
            };

            dbContext.WebhookDeliverAttempts.Add(attempt);

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            JsonNode? node = JsonNode.Parse(jsonPayload);
            node?["Error"] = ex.Message;
            jsonPayload = node?.ToJsonString();

            var attempt = new WebhookDeliveryAttempt
            {
                Id = Guid.NewGuid(),
                WebhookSubscriptionId = context.Message.SubscriptionId,
                Payload = jsonPayload!,
                ResponseStatusCode = null,
                Success = false,
                Timestamp = DateTime.UtcNow
            };

            dbContext.WebhookDeliverAttempts.Add(attempt);

            await dbContext.SaveChangesAsync();
        }
    }
}