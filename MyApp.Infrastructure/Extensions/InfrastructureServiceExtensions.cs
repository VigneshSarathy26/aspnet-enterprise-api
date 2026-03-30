using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Application.Interfaces;
using MyApp.Infrastructure.Caching;
using MyApp.Infrastructure.Persistence;
using MyApp.Infrastructure.Repositories;
using MyApp.Infrastructure.Services;
using StackExchange.Redis;

namespace MyApp.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        // ── PostgreSQL / EF Core ───────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql
                    .EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null)
                    .CommandTimeout(30)));

        // ── Redis ─────────────────────────────────────────────────────────────
        var redisConn = configuration.GetConnectionString("Redis")
                     ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConn));
        services.AddSingleton<ICacheService, RedisCacheService>();

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<IBillingRepository, BillingRepository>();

        // ── Normalizers (Strategy pattern) ───────────────────────────────────
        services.AddSingleton<IProviderNormalizer, AwsBillingNormalizer>();
        services.AddSingleton<IProviderNormalizer, AzureBillingNormalizer>();
        services.AddSingleton<IProviderNormalizer, GcpBillingNormalizer>();
        services.AddSingleton<IBillingNormalizerFactory, BillingNormalizerFactory>();

        return services;
    }
}
