namespace Webhooks.Api.Services;

internal sealed record WebhookDispatched(string EventType, object Data);