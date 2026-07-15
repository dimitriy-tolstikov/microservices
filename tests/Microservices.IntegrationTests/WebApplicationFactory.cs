using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderService.Data;
using Shared.Messaging;

namespace Microservices.IntegrationTests;

public class IntegrationTestWebApplicationFactory : WebApplicationFactory<global::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<OrderDbContext>(options => options.UseInMemoryDatabase("IntegrationTests"));

            services.RemoveAll(typeof(IKafkaProducer));
            services.AddSingleton<IKafkaProducer, FakeKafkaProducer>();
        });
    }
}
