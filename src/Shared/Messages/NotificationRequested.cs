namespace Shared.Messages;

public class NotificationRequested
{
    public Guid OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
