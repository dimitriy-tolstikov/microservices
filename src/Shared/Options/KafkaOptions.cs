using System.ComponentModel.DataAnnotations;
using Shared.Events;

namespace Shared.Options;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    [Required]
    public string BootstrapServers { get; set; } = string.Empty;

    public string GroupId { get; set; } = "default-group";

    public string Topic { get; set; } = EventTypes.OrdersTopic;

    public bool EnableAutoCommit { get; set; } = false;

    public int RetryBackoffMs { get; set; } = 1000;

    public int MessageSendMaxRetries { get; set; } = 3;
}
