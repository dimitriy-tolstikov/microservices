using System.ComponentModel.DataAnnotations;

namespace Shared.Options;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    [Required]
    public string HostName { get; set; } = "localhost";

    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string VirtualHost { get; set; } = "/";

    public int Port { get; set; } = 5672;

    public bool DispatchConsumersAsync { get; set; } = true;
}
