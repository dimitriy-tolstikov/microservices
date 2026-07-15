using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Microservices.IntegrationTests;

public class HealthCheckTests : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private readonly WebApplicationFactory<global::Program> _factory;

    public HealthCheckTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Endpoint_Should_Return_Ok()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
