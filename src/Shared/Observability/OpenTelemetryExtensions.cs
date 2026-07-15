using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.Observability;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddMicroservicesTelemetry(
        this IServiceCollection services,
        string serviceName)
    {
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddSource(serviceName)
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter();

#if ASPNETCORE
                builder.AddAspNetCoreInstrumentation();
#endif
            });

        return services;
    }
}
