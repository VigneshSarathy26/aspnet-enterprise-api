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
        // ── Relational Database / EF Core ──────────────────────────────────
        var provider = configuration["DatabaseProvider"] ?? "Postgres";

        services.AddDbContext<AppDbContext>(options =>
        {
            switch (provider)
            {
                case "SqlServer":
                    options.UseSqlServer(
                        configuration.GetConnectionString("SqlServer"),
                        sql => sql.MigrationsAssembly("MyApp.Infrastructure")
                                  .EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
                                  .CommandTimeout(30));
                    break;

                case "Oracle":
                    options.UseOracle(
                        configuration.GetConnectionString("Oracle"),
                        oracle => oracle.MigrationsAssembly("MyApp.Infrastructure")
                                        .CommandTimeout(30));
                    break;

                case "Postgres":
                default:
                    options.UseNpgsql(
                        configuration.GetConnectionString("Postgres"),
                        npgsql => npgsql.MigrationsAssembly("MyApp.Infrastructure")
                                        .EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
                                        .CommandTimeout(30));
                    break;
            }
        });

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
