namespace Webhooks.Contract;

public sealed record WebhookDispatched(string EventType, object Data);
