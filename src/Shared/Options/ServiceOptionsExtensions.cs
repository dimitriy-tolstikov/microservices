using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shared.Options;

public static class ServiceOptionsExtensions
{
    public static IServiceCollection AddKafkaOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.AddSingleton<IValidateOptions<KafkaOptions>, KafkaOptionsValidator>();
        services.AddOptions<KafkaOptions>().ValidateOnStart();
        return services;
    }

    public static IServiceCollection AddRabbitMqOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IValidateOptions<RabbitMqOptions>, RabbitMqOptionsValidator>();
        services.AddOptions<RabbitMqOptions>().ValidateOnStart();
        return services;
    }

    private sealed class KafkaOptionsValidator : IValidateOptions<KafkaOptions>
    {
        public ValidateOptionsResult Validate(string? name, KafkaOptions options)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.BootstrapServers))
            {
                failures.Add($"{nameof(KafkaOptions.BootstrapServers)} is required.");
            }

            if (string.IsNullOrWhiteSpace(options.GroupId))
            {
                failures.Add($"{nameof(KafkaOptions.GroupId)} is required.");
            }

            if (string.IsNullOrWhiteSpace(options.Topic))
            {
                failures.Add($"{nameof(KafkaOptions.Topic)} is required.");
            }

            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }
    }

    private sealed class RabbitMqOptionsValidator : IValidateOptions<RabbitMqOptions>
    {
        public ValidateOptionsResult Validate(string? name, RabbitMqOptions options)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.HostName))
            {
                failures.Add($"{nameof(RabbitMqOptions.HostName)} is required.");
            }

            if (options.Port <= 0)
            {
                failures.Add($"{nameof(RabbitMqOptions.Port)} must be greater than 0.");
            }

            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }
    }
}
