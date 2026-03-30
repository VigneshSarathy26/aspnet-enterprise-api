using FluentAssertions;
using MyApp.Domain.Enums;
using MyApp.Infrastructure.Services;
using Xunit;
using System.Text.Json;

namespace MyApp.Tests.Unit.Infrastructure;

public sealed class ProviderNormalizerTests
{
    // ── AWS ────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task AwsNormalizer_ValidPayload_MapsCorrectly()
    {
        var payload = JsonSerializer.Serialize(new
        {
            lineItem_UnblendedCost  = (object)"12.3456",     // note: real CUR uses '/' but test uses _
        });

        // Use real AWS CUR field names
        payload = """
        {
          "lineItem/UnblendedCost":  "12.3456",
          "lineItem/CurrencyCode":   "USD",
          "lineItem/UsageAmount":    "100",
          "lineItem/UsageUnit":      "Hrs",
          "lineItem/ProductCode":    "AmazonEC2",
          "product/region":          "us-east-1",
          "lineItem/UsageStartDate": "2024-01-01T00:00:00Z",
          "lineItem/UsageEndDate":   "2024-01-02T00:00:00Z"
        }
        """;

        var normalizer = new AwsBillingNormalizer();
        var record     = await normalizer.NormalizeAsync(payload, "123456789012");

        record.Provider.Should().Be(CloudProvider.AWS);
        record.Cost.Amount.Should().Be(12.3456m);
        record.Cost.Currency.Should().Be("USD");
        record.Service.ServiceName.Should().Be("AmazonEC2");
        record.Service.Region.Should().Be("us-east-1");
        record.AccountId.Should().Be("123456789012");
        record.PeriodStart.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    // ── Azure ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task AzureNormalizer_ValidPayload_MapsCorrectly()
    {
        var payload = """
        {
          "ExtendedCost":     2.304,
          "Currency":         "USD",
          "ConsumedQuantity": 24,
          "UnitOfMeasure":    "Hours",
          "ServiceName":      "Virtual Machines",
          "ResourceLocation": "eastus",
          "UsageDateTime":    "2024-01-01T00:00:00Z",
          "Tags":             { "environment": "production" }
        }
        """;

        var normalizer = new AzureBillingNormalizer();
        var record     = await normalizer.NormalizeAsync(payload, "sub-12345678");

        record.Provider.Should().Be(CloudProvider.Azure);
        record.Cost.Amount.Should().Be(2.304m);
        record.Service.ServiceName.Should().Be("Virtual Machines");
        record.Service.Region.Should().Be("eastus");
        record.Tags.Should().ContainKey("environment").WhoseValue.Should().Be("production");
    }

    // ── GCP ────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task GcpNormalizer_ValidPayload_MapsCorrectly()
    {
        var payload = """
        {
          "billing_account_id": "ABCDEF-123456",
          "service":   { "id": "95FF-2EF5-5EA1", "description": "Compute Engine" },
          "sku":       { "id": "1234-5678", "description": "N1 Predefined Instance Core" },
          "location":  { "region": "us-central1", "location": "us-central1" },
          "cost":      0.048,
          "currency":  "USD",
          "usage":     { "amount": 1.0, "unit": "h" },
          "usage_start_time": "2024-01-01T00:00:00Z",
          "usage_end_time":   "2024-01-02T00:00:00Z",
          "labels": [
            { "key": "environment", "value": "prod" }
          ]
        }
        """;

        var normalizer = new GcpBillingNormalizer();
        var record     = await normalizer.NormalizeAsync(payload, "my-project-123");

        record.Provider.Should().Be(CloudProvider.GCP);
        record.Cost.Amount.Should().Be(0.048m);
        record.Service.Region.Should().Be("us-central1");
        record.Service.ServiceName.Should().Contain("Compute Engine");
        record.Tags.Should().ContainKey("environment").WhoseValue.Should().Be("prod");
    }
}
