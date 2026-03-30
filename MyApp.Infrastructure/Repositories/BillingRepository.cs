using Microsoft.EntityFrameworkCore;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Domain.Enums;
using MyApp.Infrastructure.Persistence;

namespace MyApp.Infrastructure.Repositories;

public sealed class BillingRepository : IBillingRepository
{
    private readonly AppDbContext _db;

    public BillingRepository(AppDbContext db) => _db = db;

    public async Task<BillingRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.BillingRecords.FindAsync([id], ct);

    public async Task<(IReadOnlyList<BillingRecord> Records, int TotalCount)> GetPagedAsync(
        BillingRecordsFilter filter, CancellationToken ct = default)
    {
        var query = _db.BillingRecords.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.AccountId))
            query = query.Where(r => r.AccountId == filter.AccountId);

        if (filter.Provider.HasValue)
            query = query.Where(r => r.Provider == filter.Provider.Value);

        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status.Value);

        if (filter.From.HasValue)
            query = query.Where(r => r.PeriodStart >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(r => r.PeriodEnd <= filter.To.Value);

        var total   = await query.CountAsync(ct);
        var records = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return (records, total);
    }

    public async Task<BillingAggregateResult> ComputeAggregateAsync(
        string accountId, CloudProvider? provider,
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var query = _db.BillingRecords
            .AsNoTracking()
            .Where(r => r.AccountId == accountId
                     && r.PeriodStart >= from
                     && r.PeriodEnd   <= to
                     && r.Status      == BillingStatus.Normalized);

        if (provider.HasValue)
            query = query.Where(r => r.Provider == provider.Value);

        var result = await query
            .GroupBy(r => new { r.AccountId, Provider = r.Provider.ToString() })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.Provider,
                TotalCost   = g.Sum(r => r.Cost.Amount),
                Currency    = g.First().Cost.Currency,
                RecordCount = g.Count()
            })
            .FirstOrDefaultAsync(ct);

        return new BillingAggregateResult
        {
            AccountId   = accountId,
            Provider    = result?.Provider ?? provider?.ToString() ?? "All",
            TotalCost   = result?.TotalCost   ?? 0,
            Currency    = result?.Currency    ?? "USD",
            RecordCount = result?.RecordCount ?? 0,
            PeriodStart = from,
            PeriodEnd   = to
        };
    }

    public async Task AddAsync(BillingRecord record, CancellationToken ct = default)
        => await _db.BillingRecords.AddAsync(record, ct);

    public async Task AddRangeAsync(IReadOnlyList<BillingRecord> records, CancellationToken ct = default)
        => await _db.BillingRecords.AddRangeAsync(records, ct);

    public Task UpdateAsync(BillingRecord record, CancellationToken ct = default)
    {
        _db.BillingRecords.Update(record);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
