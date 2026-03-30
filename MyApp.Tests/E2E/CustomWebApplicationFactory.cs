using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyApp.Infrastructure.Persistence;
using System.Linq;

namespace MyApp.Tests.E2E;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the existing PostgreSQL DbContext mapping
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add Entity Framework In-Memory Database for testing
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Register a dummy mock for ICacheService
            var mockCache = new Moq.Mock<MyApp.Application.Interfaces.ICacheService>();
            services.AddSingleton(mockCache.Object);

            // Removing actual Redis multiplexer to prevent healthcheck hangs
            var redisDesc = services.SingleOrDefault(d => d.ServiceType == typeof(StackExchange.Redis.IConnectionMultiplexer));
            if (redisDesc != null) services.Remove(redisDesc);
        });
    }
}
