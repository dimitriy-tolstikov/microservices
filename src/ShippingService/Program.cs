using ShippingService.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ShippingWorker>();

var host = builder.Build();
host.Run();
