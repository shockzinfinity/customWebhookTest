using Webhooks.Api.Models;

namespace Webhooks.Api.Repositories;

public class InMemoryWebhookSubscriptionRepository
{
    private readonly List<WebhookSubscription> _subscriptions = new();

    public void Add(WebhookSubscription subscription)
    {
        _subscriptions.Add(subscription);
    }

    public IReadOnlyList<WebhookSubscription> GetByEventType(string eventType)
    {
        return _subscriptions.Where(s => s.EventType == eventType).ToList().AsReadOnly();
    }
}