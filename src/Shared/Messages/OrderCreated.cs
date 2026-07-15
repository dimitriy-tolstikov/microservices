namespace Shared.Messages;

public class OrderCreated
{
    public Guid OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
