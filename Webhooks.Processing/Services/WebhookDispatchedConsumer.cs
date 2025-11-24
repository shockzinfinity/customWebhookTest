using MassTransit;
using Microsoft.EntityFrameworkCore;
using Webhooks.Contract;
using Webhooks.Processing.Data;

namespace Webhooks.Processing.Services;

internal sealed class WebhookDispatchedConsumer(WebhooksDbContext dbContext) : IConsumer<WebhookDispatched>
{
    public async Task Consume(ConsumeContext<WebhookDispatched> context)
    {
        var message = context.Message;
        var subscriptions = await dbContext.WebhookSubscriptions.AsNoTracking().Where(s => s.EventType == message.EventType).ToListAsync();

        foreach (var subscription in subscriptions)
        {
            await context.Publish(new WebhookTriggered(
                subscription.Id,
                subscription.EventType,
                subscription.WebhookUrl,
                message.Data));
        }

        //await context.PublishBatch(subscriptions.Where(s => new WebhookTriggered(
        //    s.Id,
        //    s.EventType,
        //    s.WebhookUrl,
        //    message.Data)));
    }
}