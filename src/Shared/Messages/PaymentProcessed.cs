namespace Shared.Messages;

public class PaymentProcessed
{
    public Guid OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
