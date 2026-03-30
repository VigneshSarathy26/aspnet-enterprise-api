using System.Text.Json;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Domain.Enums;
using MyApp.Domain.ValueObjects;

namespace MyApp.Infrastructure.Services;

/// <summary>
/// Normalizes Azure Cost Management export JSON payloads into canonical BillingRecords.
/// Expected fields: SubscriptionId, ServiceName, ResourceLocation, ExtendedCost, Currency, Date, etc.
/// </summary>
public sealed class AzureBillingNormalizer : IProviderNormalizer
{
    public CloudProvider SupportedProvider => CloudProvider.Azure;

    public Task<BillingRecord> NormalizeAsync(
        string rawPayload, string accountId, string? correlationId = null, CancellationToken ct = default)
    {
        var doc = JsonDocument.Parse(rawPayload).RootElement;

        var cost = GetDecimal(doc, "ExtendedCost") ?? GetDecimal(doc, "PreTaxCost") ?? 0m;
        var usage = GetDecimal(doc, "ConsumedQuantity") ?? 0m;
        var currency    = GetString(doc, "Currency")          ?? "USD";
        var serviceName = GetString(doc, "ServiceName")       ?? GetString(doc, "MeterCategory") ?? "Unknown";
        var region      = GetString(doc, "ResourceLocation")  ?? GetString(doc, "ServiceRegion") ?? "eastus";
        var usageUnit   = GetString(doc, "UnitOfMeasure")     ?? "Units";
        var dateStr     = GetString(doc, "UsageDateTime")     ?? GetString(doc, "Date") ?? DateTime.UtcNow.ToString("o");
        var periodStart = DateTime.TryParse(dateStr, out var ds) ? ds.ToUniversalTime() : DateTime.UtcNow.Date;
        var periodEnd   = periodStart.AddDays(1);

        // Azure tags come as a JSON object
        var tags = new Dictionary<string, string>();
        if (doc.TryGetProperty("Tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Object)
            foreach (var p in tagsEl.EnumerateObject())
                tags[p.Name] = p.Value.GetString() ?? "";

        var record = BillingRecord.Create(
            accountId     : accountId,
            provider      : CloudProvider.Azure,
            service       : new ServiceIdentifier("Azure", serviceName, region),
            cost          : new MoneyAmount(cost, currency),
            usageQuantity : new MoneyAmount(usage, currency),
            usageUnit     : usageUnit,
            periodStart   : periodStart,
            periodEnd     : periodEnd,
            rawPayload    : rawPayload,
            correlationId : correlationId,
            tags          : tags);

        return Task.FromResult(record);
    }

    public async Task<IReadOnlyList<BillingRecord>> NormalizeBatchAsync(
        IEnumerable<string> rawPayloads, string accountId,
        string? correlationId = null, CancellationToken ct = default)
    {
        var results = new List<BillingRecord>();
        foreach (var payload in rawPayloads)
            results.Add(await NormalizeAsync(payload, accountId, correlationId, ct));
        return results;
    }

    private static string? GetString(JsonElement el, string key)
        => el.TryGetProperty(key, out var v) ? v.GetString() : null;

    private static decimal? GetDecimal(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var v)) return null;
        return v.ValueKind == JsonValueKind.Number ? v.GetDecimal() :
               decimal.TryParse(v.GetString(), System.Globalization.NumberStyles.Any,
                   System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
    }
}
