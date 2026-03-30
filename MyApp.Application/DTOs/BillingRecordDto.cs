namespace MyApp.Application.DTOs;

public sealed class BillingRecordDto
{
    public Guid     Id            { get; init; }
    public string   AccountId     { get; init; } = default!;
    public string   Provider      { get; init; } = default!;
    public string   ServiceName   { get; init; } = default!;
    public string   Region        { get; init; } = default!;
    public decimal  Cost          { get; init; }
    public string   Currency      { get; init; } = default!;
    public decimal  UsageQuantity { get; init; }
    public string   UsageUnit     { get; init; } = default!;
    public DateTime PeriodStart   { get; init; }
    public DateTime PeriodEnd     { get; init; }
    public string   Status        { get; init; } = default!;
    public string?  CorrelationId { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new();
    public DateTime CreatedAt     { get; init; }
    public DateTime? UpdatedAt    { get; init; }
}
