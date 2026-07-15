using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using OrderService.Services;
using Shared.Messages;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderPublisher _publisher;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderPublisher publisher, ILogger<OrdersController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<OrderCreated>> Create([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var order = new OrderCreated
        {
            OrderId = Guid.NewGuid(),
            CustomerEmail = request.CustomerEmail,
            Product = request.Product,
            Quantity = request.Quantity,
            Amount = request.Amount
        };

        await _publisher.PublishAsync(order);

        _logger.LogInformation("Order {OrderId} accepted for {CustomerEmail}", order.OrderId, order.CustomerEmail);

        return AcceptedAtAction(
            nameof(GetById),
            new { id = order.OrderId },
            order);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<OrderCreated> GetById(Guid id)
    {
        // В учебном примере просто возвращаем заглушку.
        return Ok(new { OrderId = id, Status = "Processing" });
    }
}
