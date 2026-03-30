using System.Text.Json;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Domain.Enums;
using MyApp.Domain.ValueObjects;

namespace MyApp.Infrastructure.Services;

/// <summary>
/// Normalizes AWS Cost and Usage Report (CUR) JSON payloads into canonical BillingRecords.
/// Expected fields: lineItem/UsageAccountId, lineItem/UnblendedCost, lineItem/CurrencyCode,
/// lineItem/UsageAmount, lineItem/UsageType, product/region, etc.
/// </summary>
public sealed class AwsBillingNormalizer : IProviderNormalizer
{
    public CloudProvider SupportedProvider => CloudProvider.AWS;

    public Task<BillingRecord> NormalizeAsync(
        string rawPayload, string accountId, string? correlationId = null, CancellationToken ct = default)
    {
        var doc = JsonDocument.Parse(rawPayload).RootElement;

        var cost = decimal.TryParse(
            GetString(doc, "lineItem/UnblendedCost"), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var c) ? c : 0m;

        var usage = decimal.TryParse(
            GetString(doc, "lineItem/UsageAmount"), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var u) ? u : 0m;

        var currency    = GetString(doc, "lineItem/CurrencyCode") ?? "USD";
        var serviceName = GetString(doc, "product/servicecode") ?? GetString(doc, "lineItem/ProductCode") ?? "Unknown";
        var region      = GetString(doc, "product/region")      ?? "us-east-1";
        var usageType   = GetString(doc, "lineItem/UsageType")   ?? "Unknown";
        var periodStart = GetDateTime(doc, "lineItem/UsageStartDate") ?? DateTime.UtcNow.Date;
        var periodEnd   = GetDateTime(doc, "lineItem/UsageEndDate")   ?? periodStart.AddDays(1);
        var usageUnit   = GetString(doc, "lineItem/UsageUnit")        ?? "Hrs";

        var tags = doc.TryGetProperty("resourceTags", out var tagsEl)
            ? tagsEl.EnumerateObject().ToDictionary(p => p.Name.Replace("user:", ""), p => p.Value.GetString() ?? "")
            : new Dictionary<string, string>();

        var record = BillingRecord.Create(
            accountId     : accountId,
            provider      : CloudProvider.AWS,
            service       : new ServiceIdentifier("AWS", serviceName, region),
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

    private static DateTime? GetDateTime(JsonElement el, string key)
        => el.TryGetProperty(key, out var v) && DateTime.TryParse(v.GetString(), out var dt)
            ? dt.ToUniversalTime() : null;
}
