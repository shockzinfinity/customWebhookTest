using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("webhooks");

builder.AddProject<Webhooks_Api>("webhooks-api")
    .WithReference(database)
    .WaitFor(database);

builder.AddProject<Projects.WebhookClient>("webhookclient");

builder.Build().Run();
