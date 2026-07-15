using System.ComponentModel.DataAnnotations;

namespace OrderService.Models;

public class CreateOrderRequest
{
    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    public string Product { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
}
