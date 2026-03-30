namespace MyApp.Application.Interfaces;

/// <summary>Aggregated billing cost result computed from BillingRecord data.</summary>
public sealed class BillingAggregateResult
{
    public string   AccountId   { get; init; } = default!;
    public string   Provider    { get; init; } = default!;
    public decimal  TotalCost   { get; init; }
    public string   Currency    { get; init; } = default!;
    public int      RecordCount { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd   { get; init; }
}
