using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using Shared.Messages;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrderDbContext dbContext, ILogger<OrdersController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<OrderCreated>> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerEmail = request.CustomerEmail,
            Product = request.Product,
            Quantity = request.Quantity,
            Amount = request.Amount,
            Status = "Processing"
        };

        var orderCreatedEvent = new OrderCreated
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            Product = order.Product,
            Quantity = order.Quantity,
            Amount = order.Amount
        };

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(OrderCreated).FullName!,
            Payload = System.Text.Json.JsonSerializer.Serialize(orderCreatedEvent)
        };

        _dbContext.Orders.Add(order);
        _dbContext.OutboxMessages.Add(outboxMessage);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Order {OrderId} accepted for {CustomerEmail}", order.Id, order.CustomerEmail);

        return AcceptedAtAction(
            nameof(GetById),
            new { id = order.Id },
            orderCreatedEvent);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Order>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (order == null)
        {
            return NotFound();
        }

        return Ok(order);
    }
}
