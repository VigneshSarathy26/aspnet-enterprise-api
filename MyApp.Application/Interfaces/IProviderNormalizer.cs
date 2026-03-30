using MyApp.Domain.Entities;
using MyApp.Domain.Enums;

namespace MyApp.Application.Interfaces;

/// <summary>Normalizes a raw cloud provider payload into a canonical BillingRecord.</summary>
public interface IProviderNormalizer
{
    CloudProvider SupportedProvider { get; }

    Task<BillingRecord> NormalizeAsync(
        string rawPayload, string accountId,
        string? correlationId = null, CancellationToken ct = default);

    Task<IReadOnlyList<BillingRecord>> NormalizeBatchAsync(
        IEnumerable<string> rawPayloads, string accountId,
        string? correlationId = null, CancellationToken ct = default);
}
