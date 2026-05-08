using OrderSaga.Workflows;
using Temporalio.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedTemporalWorker(
    "localhost:7233",
    "default",
    "order-saga-task-queue")
    .AddScopedActivities<OrderSagaActivities>()
    .AddWorkflow<OrderSagaWorkflow>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Order Saga Worker is running");

app.Run();


