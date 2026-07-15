using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using NotificationService.Workers;
using Shared.Messaging;
using Shared.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRabbitMqOptions(builder.Configuration);
builder.Services.AddSingleton<IMessageSerializer, SystemTextJsonMessageSerializer>();
builder.Services.AddHostedService<NotificationWorker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("notification-service"))
            .AddSource("notification-service")
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });

var host = builder.Build();
host.Run();
