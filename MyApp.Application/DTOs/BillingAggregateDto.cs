namespace MyApp.Application.DTOs;

public sealed class BillingAggregateDto
{
    public string   AccountId   { get; init; } = default!;
    public string   Provider    { get; init; } = default!;
    public decimal  TotalCost   { get; init; }
    public string   Currency    { get; init; } = default!;
    public int      RecordCount { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd   { get; init; }
}

public sealed class IngestionResponseDto
{
    public Guid   RecordId      { get; init; }
    public string Status        { get; init; } = default!;
    public string Provider      { get; init; } = default!;
    public string AccountId     { get; init; } = default!;
    public string? CorrelationId { get; init; }
    public DateTime IngestedAt  { get; init; } = DateTime.UtcNow;
}

public sealed class BatchIngestionResponseDto
{
    public int              SucceededCount { get; init; }
    public int              FailedCount    { get; init; }
    public IReadOnlyList<Guid>   RecordIds { get; init; } = [];
    public IReadOnlyList<string> Errors    { get; init; } = [];
}
