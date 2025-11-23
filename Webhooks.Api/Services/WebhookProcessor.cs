using System.Diagnostics;
using System.Threading.Channels;
using Webhooks.Api.OpenTelemetry;

namespace Webhooks.Api.Services;

internal sealed class WebhookProcessor(IServiceScopeFactory scopeFactory, Channel<WebhookDispatch> webhooksChannel) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (WebhookDispatch dispatch in webhooksChannel.Reader.ReadAllAsync(stoppingToken))
        {
            using Activity? activity = DiagnosticConfig.Source.StartActivity(
                $"{dispatch.EventType} process webhook",
                ActivityKind.Internal,
                parentId: dispatch.ParentActivityId);

            using IServiceScope scope = scopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<WebhookDispatcher>();

            await dispatcher.ProcessAsync(dispatch.EventType, dispatch.Data);
        }
    }
}