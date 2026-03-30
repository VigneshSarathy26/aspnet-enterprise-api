using MyApp.Domain.Common;
using MyApp.Domain.Enums;
using MyApp.Domain.Events;
using MyApp.Domain.ValueObjects;

namespace MyApp.Domain.Entities;

/// <summary>
/// Aggregate root representing a single cloud billing line item after ingestion.
/// Lifecycle: Pending → Normalized | Failed → Archived.
/// </summary>
public sealed class BillingRecord : BaseEntity
{
    public string           AccountId     { get; private set; } = default!;
    public CloudProvider    Provider      { get; private set; }
    public ServiceIdentifier Service      { get; private set; } = default!;
    public MoneyAmount      Cost          { get; private set; } = default!;
    public MoneyAmount      UsageQuantity { get; private set; } = default!;
    public string           UsageUnit     { get; private set; } = default!;
    public DateTime         PeriodStart   { get; private set; }
    public DateTime         PeriodEnd     { get; private set; }
    public BillingStatus    Status        { get; private set; }
    public string           RawPayload    { get; private set; } = default!;
    public string?          CorrelationId { get; private set; }
    public string?          FailureReason { get; private set; }
    public Dictionary<string, string> Tags { get; private set; } = new();

    // EF Core
    private BillingRecord() { }

    public static BillingRecord Create(
        string            accountId,
        CloudProvider     provider,
        ServiceIdentifier service,
        MoneyAmount       cost,
        MoneyAmount       usageQuantity,
        string            usageUnit,
        DateTime          periodStart,
        DateTime          periodEnd,
        string            rawPayload,
        string?           correlationId = null,
        Dictionary<string, string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(accountId))  throw new ArgumentException("AccountId is required.", nameof(accountId));
        if (string.IsNullOrWhiteSpace(usageUnit))   throw new ArgumentException("UsageUnit is required.", nameof(usageUnit));
        if (periodStart >= periodEnd)               throw new ArgumentException("PeriodStart must be before PeriodEnd.");
        if (string.IsNullOrWhiteSpace(rawPayload))  throw new ArgumentException("RawPayload is required.", nameof(rawPayload));

        var record = new BillingRecord
        {
            AccountId     = accountId,
            Provider      = provider,
            Service       = service       ?? throw new ArgumentNullException(nameof(service)),
            Cost          = cost          ?? throw new ArgumentNullException(nameof(cost)),
            UsageQuantity = usageQuantity ?? throw new ArgumentNullException(nameof(usageQuantity)),
            UsageUnit     = usageUnit,
            PeriodStart   = periodStart,
            PeriodEnd     = periodEnd,
            Status        = BillingStatus.Pending,
            RawPayload    = rawPayload,
            CorrelationId = correlationId,
            Tags          = tags ?? new Dictionary<string, string>()
        };

        record.AddDomainEvent(new BillingRecordIngested(record.Id, provider, cost));
        return record;
    }

    public void MarkNormalized()
    {
        Status = BillingStatus.Normalized;
        SetUpdatedAt();
    }

    public void MarkFailed(string reason)
    {
        Status        = BillingStatus.Failed;
        FailureReason = reason;
        SetUpdatedAt();
    }

    public void Archive()
    {
        Status = BillingStatus.Archived;
        SetUpdatedAt();
    }
}
