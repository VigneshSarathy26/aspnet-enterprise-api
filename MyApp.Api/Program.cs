using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using MyApp.Api.Middleware;
using MyApp.Application;
using MyApp.Infrastructure.Extensions;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;

// ── Bootstrap Serilog early ──────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ──────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .Enrich.WithMachineName()
           .Enrich.WithThreadId()
           .WriteTo.Console(outputTemplate:
               "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}"));

    // ── Application + Infrastructure ─────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── Controllers ──────────────────────────────────────────────────────────
    builder.Services.AddControllers()
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.PropertyNamingPolicy  = System.Text.Json.JsonNamingPolicy.CamelCase;
            o.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    // ── Swagger / OpenAPI ────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "Cloud Billing Telemetry API",
            Version     = "v1",
            Description = "Enterprise-grade microservice for ingesting, normalizing, and serving " +
                          "cloud billing telemetry from AWS, Azure, and GCP."
        });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name         = "Authorization",
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "JWT Bearer token"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                []
            }
        });
    });

    // ── JWT Authentication ───────────────────────────────────────────────────
    builder.Services
        .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwt = builder.Configuration.GetSection("Jwt");
            options.Authority = jwt["Authority"];
            options.Audience  = jwt["Audience"];
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        });

    builder.Services.AddAuthorization();

    // ── Rate Limiting ────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(opts =>
    {
        opts.AddSlidingWindowLimiter("ingest", o =>
        {
            o.Window            = TimeSpan.FromSeconds(10);
            o.PermitLimit       = int.Parse(builder.Configuration["RateLimiting:IngestPermitLimit"] ?? "500");
            o.SegmentsPerWindow = 5;
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            o.QueueLimit        = 100;
        });

        opts.AddFixedWindowLimiter("query", o =>
        {
            o.Window      = TimeSpan.FromSeconds(1);
            o.PermitLimit = int.Parse(builder.Configuration["RateLimiting:QueryPermitLimit"] ?? "200");
        });

        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // ── OpenTelemetry ────────────────────────────────────────────────────────
    var otlpEndpoint = builder.Configuration["OpenTelemetry:Endpoint"];
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("cloud-billing-telemetry-api"))
        .WithTracing(t =>
        {
            t.AddAspNetCoreInstrumentation()
             .AddHttpClientInstrumentation();
            if (!string.IsNullOrEmpty(otlpEndpoint))
                t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        })
        .WithMetrics(m =>
        {
            m.AddAspNetCoreInstrumentation()
             .AddHttpClientInstrumentation()
             .AddRuntimeInstrumentation()
             .AddPrometheusExporter();
        });

    // ── Health Checks ────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!)
        .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");

    // ════════════════════════════════════════════════════════════════════════
    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging(o =>
    {
        o.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cloud Billing Telemetry API v1");
            c.RoutePrefix = string.Empty;   // Swagger at root
        });
    }

    app.UseRateLimiter();

    // Apply rate limiters to ingestion endpoints
    app.MapControllers()
       .RequireAuthorization();

    app.MapHealthChecks("/health");
    app.MapPrometheusScrapingEndpoint("/metrics");

    // Auto-apply EF Core migrations on startup (dev/staging)
    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider
                      .GetRequiredService<MyApp.Infrastructure.Persistence.AppDbContext>();
        await db.Database.MigrateAsync();
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not OperationCanceledException && ex.GetType().Name != "StopTheHostException")
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }
