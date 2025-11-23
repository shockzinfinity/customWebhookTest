using MassTransit;
using MassTransit.Logging;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Webhooks.Api.Data;
using Webhooks.Api.Extensions;
using Webhooks.Api.Models;
using Webhooks.Api.OpenTelemetry;
using Webhooks.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<WebhookDispatcher>();

builder.Services.AddDbContext<WebhooksDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("webhooks")));

//builder.Services.AddHostedService<WebhookProcessor>();

//builder.Services.AddSingleton(_ =>
//{
//    return Channel.CreateBounded<WebhookDispatch>(new BoundedChannelOptions(100)
//    {
//        FullMode = BoundedChannelFullMode.Wait
//    });
//});

builder.Services.AddMassTransit(busConfig =>
{
    busConfig.SetKebabCaseEndpointNameFormatter();

    busConfig.AddConsumer<WebhookDispatchedConsumer>();
    busConfig.AddConsumer<WebhookTriggeredConsumer>();

    busConfig.UsingRabbitMq((context, config) =>
    {
        config.Host(builder.Configuration.GetConnectionString("rabbitmq"));
        config.ConfigureEndpoints(context);
    });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracsing =>
    {
        object value = tracsing.AddSource(DiagnosticConfig.Source.Name)
        .AddSource(DiagnosticConfig.Source.Name)
        .AddSource(DiagnosticHeaders.DefaultListenerName)
        .AddNpgsql();
    })
    .WithTracing(tracing =>
    {
    });

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "OpenAPI V1");
    });

    await app.ApplyMigrationsAsync();
}

app.UseHttpsRedirection();

app.MapPost("/orders", async (CreateOrderRequest request, WebhooksDbContext db, WebhookDispatcher dispatcher) =>
{
    var order = new Order(Guid.NewGuid(), request.CustomerName, request.Amount, DateTime.UtcNow);
    db.Orders.Add(order);
    await db.SaveChangesAsync();

    await dispatcher.DispatchAsync("order.created", order);

    return Results.Ok(order);
}).WithTags("Orders");

app.MapGet("/orders", async (WebhooksDbContext dbContext) =>
{
    return Results.Ok(await dbContext.Orders.ToListAsync());
}).WithTags("Orders");

app.MapPost("/webhooks/subscriptions", async (CreateWebhookRequest request, WebhooksDbContext db) =>
{
    var subscription = new WebhookSubscription(
        Guid.NewGuid(),
        request.EventType,
        request.WebhookUrl,
        DateTime.UtcNow);

    db.WebhookSubscriptions.Add(subscription);
    await db.SaveChangesAsync();

    return Results.Ok(subscription);
});

app.Run();