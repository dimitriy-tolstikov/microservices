using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ShippingService.Workers;
using Shared.Messaging;
using Shared.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRabbitMqOptions(builder.Configuration);
builder.Services.AddSingleton<IMessageSerializer, SystemTextJsonMessageSerializer>();
builder.Services.AddHostedService<ShippingWorker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("shipping-service"))
            .AddSource("shipping-service")
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });

var host = builder.Build();
host.Run();
