namespace Shared.Messages;

public class ShippingRequested
{
    public Guid OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
