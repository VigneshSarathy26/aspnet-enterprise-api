using MyApp.Domain.Entities;
using MyApp.Domain.Enums;

namespace MyApp.Application.Interfaces;

public interface IBillingRepository
{
    Task<BillingRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(IReadOnlyList<BillingRecord> Records, int TotalCount)> GetPagedAsync(
        BillingRecordsFilter filter, CancellationToken ct = default);

    Task<BillingAggregateResult> ComputeAggregateAsync(
        string accountId, CloudProvider? provider,
        DateTime from, DateTime to, CancellationToken ct = default);

    Task AddAsync(BillingRecord record, CancellationToken ct = default);
    Task AddRangeAsync(IReadOnlyList<BillingRecord> records, CancellationToken ct = default);
    Task UpdateAsync(BillingRecord record, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
