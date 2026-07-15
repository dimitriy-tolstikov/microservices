using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OrderService.Controllers;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.UnitTests.Controllers;

public class OrdersControllerTests
{
    [Fact]
    public async Task Create_Should_Save_Order_And_OutboxMessage()
    {
        // Arrange
        var dbOptions = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new OrderDbContext(dbOptions);
        var logger = Substitute.For<ILogger<OrdersController>>();
        var controller = new OrdersController(context, logger);

        var request = new CreateOrderRequest
        {
            CustomerEmail = "user@example.com",
            Product = "Ноутбук",
            Quantity = 1,
            Amount = 5000
        };

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<AcceptedAtActionResult>();
        context.Orders.Should().ContainSingle();
        context.OutboxMessages.Should().ContainSingle();

        var order = context.Orders.Single();
        order.CustomerEmail.Should().Be(request.CustomerEmail);
        order.Amount.Should().Be(request.Amount);
    }

    [Fact]
    public async Task GetById_Should_Return_NotFound_When_Order_Does_Not_Exist()
    {
        // Arrange
        var dbOptions = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new OrderDbContext(dbOptions);
        var logger = Substitute.For<ILogger<OrdersController>>();
        var controller = new OrdersController(context, logger);

        // Act
        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }
}
