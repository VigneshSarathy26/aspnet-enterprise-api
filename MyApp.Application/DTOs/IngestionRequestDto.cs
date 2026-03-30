namespace MyApp.Application.DTOs;

public sealed class IngestionRequestDto
{
    /// <example>AWS</example>
    public string  Provider      { get; init; } = default!;
    /// <example>123456789012</example>
    public string  AccountId     { get; init; } = default!;
    /// <summary>Raw JSON payload from the cloud provider's billing export.</summary>
    public string  RawPayload    { get; init; } = default!;
    public string? CorrelationId { get; init; }
}

public sealed class BatchIngestionRequestDto
{
    public string              Provider      { get; init; } = default!;
    public string              AccountId     { get; init; } = default!;
    public IEnumerable<string> RawPayloads   { get; init; } = [];
    public string?             CorrelationId { get; init; }
}
