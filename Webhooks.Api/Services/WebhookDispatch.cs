namespace Webhooks.Api.Services;

internal sealed record WebhookDispatch(string EventType, object Data, string? ParentActivityId);