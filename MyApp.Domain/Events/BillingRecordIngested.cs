using MyApp.Domain.Common;
using MyApp.Domain.Enums;
using MyApp.Domain.ValueObjects;

namespace MyApp.Domain.Events;

/// <summary>
/// Domain event raised every time a BillingRecord is created and queued for persistence.
/// Consumers in the Application layer subscribe via DomainEventNotification&lt;BillingRecordIngested&gt;.
/// </summary>
public sealed record BillingRecordIngested(
    Guid          RecordId,
    CloudProvider Provider,
    MoneyAmount   Cost,
    DateTime      OccurredAt) : IDomainEvent
{
    public BillingRecordIngested(Guid recordId, CloudProvider provider, MoneyAmount cost)
        : this(recordId, provider, cost, DateTime.UtcNow) { }
}
