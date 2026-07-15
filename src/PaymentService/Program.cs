using PaymentService.Services;
using PaymentService.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<PaymentWorker>();
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

var host = builder.Build();
host.Run();
