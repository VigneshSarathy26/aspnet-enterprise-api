using MediatR;
using Microsoft.Extensions.Logging;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;
using MyApp.Domain.Enums;

namespace MyApp.Application.Commands.IngestBillingBatch;

public sealed class IngestBillingBatchHandler
    : IRequestHandler<IngestBillingBatchCommand, BatchIngestionResponseDto>
{
    private readonly IBillingRepository       _repository;
    private readonly IBillingNormalizerFactory _normalizers;
    private readonly ILogger<IngestBillingBatchHandler> _logger;

    public IngestBillingBatchHandler(
        IBillingRepository        repository,
        IBillingNormalizerFactory normalizers,
        ILogger<IngestBillingBatchHandler> logger)
    {
        _repository  = repository;
        _normalizers = normalizers;
        _logger      = logger;
    }

    public async Task<BatchIngestionResponseDto> Handle(
        IngestBillingBatchCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<CloudProvider>(request.Provider, ignoreCase: true, out var provider))
            throw new ArgumentException($"Unknown cloud provider: '{request.Provider}'.");

        var payloads = request.RawPayloads.ToList();
        _logger.LogInformation(
            "Batch ingesting {Count} records. Provider={Provider} AccountId={AccountId}",
            payloads.Count, provider, request.AccountId);

        var normalizer = _normalizers.GetNormalizer(provider);
        var records    = await normalizer.NormalizeBatchAsync(
                             payloads, request.AccountId, request.CorrelationId, ct);

        var errors  = new List<string>();
        var ids     = new List<Guid>();

        foreach (var record in records)
        {
            try
            {
                record.MarkNormalized();
                ids.Add(record.Id);
            }
            catch (Exception ex)
            {
                record.MarkFailed(ex.Message);
                errors.Add(ex.Message);
            }
        }

        await _repository.AddRangeAsync(records, ct);
        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Batch ingestion complete. Succeeded={S} Failed={F}", ids.Count, errors.Count);

        return new BatchIngestionResponseDto
        {
            SucceededCount = ids.Count,
            FailedCount    = errors.Count,
            RecordIds      = ids,
            Errors         = errors
        };
    }
}
