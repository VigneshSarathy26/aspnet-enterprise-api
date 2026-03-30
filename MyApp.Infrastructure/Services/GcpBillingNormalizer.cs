using System.Text.Json;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Domain.Enums;
using MyApp.Domain.ValueObjects;

namespace MyApp.Infrastructure.Services;

/// <summary>
/// Normalizes GCP Billing Export (BigQuery JSON) payloads into canonical BillingRecords.
/// Expected fields: billing_account_id, service.description, location.region, cost, currency,
/// usage.amount, usage.unit, usage_start_time, usage_end_time, labels[], etc.
/// </summary>
public sealed class GcpBillingNormalizer : IProviderNormalizer
{
    public CloudProvider SupportedProvider => CloudProvider.GCP;

    public Task<BillingRecord> NormalizeAsync(
        string rawPayload, string accountId, string? correlationId = null, CancellationToken ct = default)
    {
        var doc = JsonDocument.Parse(rawPayload).RootElement;

        var cost     = GetDecimal(doc, "cost") ?? 0m;
        var currency = GetString(doc, "currency") ?? "USD";

        // GCP nested objects
        var serviceName = doc.TryGetProperty("service", out var svc)
            ? GetString(svc, "description") ?? "Unknown" : "Unknown";
        var skuName = doc.TryGetProperty("sku", out var sku)
            ? GetString(sku, "description") : null;
        var region = doc.TryGetProperty("location", out var loc)
            ? GetString(loc, "region") ?? GetString(loc, "location") ?? "us-central1" : "us-central1";

        var usageAmount = 0m;
        var usageUnit   = "h";
        if (doc.TryGetProperty("usage", out var usageEl))
        {
            usageAmount = GetDecimal(usageEl, "amount") ?? GetDecimal(usageEl, "amount_in_pricing_units") ?? 0m;
            usageUnit   = GetString(usageEl, "unit") ?? GetString(usageEl, "pricing_unit") ?? "h";
        }

        var periodStart = GetDateTime(doc, "usage_start_time") ?? DateTime.UtcNow.Date;
        var periodEnd   = GetDateTime(doc, "usage_end_time")   ?? periodStart.AddHours(1);

        // GCP labels come as array of {key, value}
        var tags = new Dictionary<string, string>();
        if (doc.TryGetProperty("labels", out var labelsEl) && labelsEl.ValueKind == JsonValueKind.Array)
            foreach (var label in labelsEl.EnumerateArray())
            {
                var k = label.TryGetProperty("key",   out var kv) ? kv.GetString() : null;
                var v = label.TryGetProperty("value", out var vv) ? vv.GetString() : null;
                if (k is not null) tags[k] = v ?? "";
            }

        var displayName = skuName is not null ? $"{serviceName} / {skuName}" : serviceName;

        var record = BillingRecord.Create(
            accountId     : accountId,
            provider      : CloudProvider.GCP,
            service       : new ServiceIdentifier("GCP", displayName, region),
            cost          : new MoneyAmount(cost, currency),
            usageQuantity : new MoneyAmount(usageAmount, currency),
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

    private static DateTime? GetDateTime(JsonElement el, string key)
        => el.TryGetProperty(key, out var v) && DateTime.TryParse(v.GetString(), out var dt)
            ? dt.ToUniversalTime() : null;
}
