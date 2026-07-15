using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PaymentService.Workers;
using Shared.Idempotency;
using Shared.Messaging;
using Shared.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddKafkaOptions(builder.Configuration);
builder.Services.AddRabbitMqOptions(builder.Configuration);

builder.Services.AddSingleton<IMessageSerializer, SystemTextJsonMessageSerializer>();
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

builder.Services.AddHostedService<PaymentWorker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("payment-service"))
            .AddSource("payment-service")
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });

var host = builder.Build();
host.Run();
