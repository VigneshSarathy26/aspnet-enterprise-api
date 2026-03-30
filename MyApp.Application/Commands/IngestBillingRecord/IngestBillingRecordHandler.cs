using MediatR;
using Microsoft.Extensions.Logging;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;
using MyApp.Domain.Enums;

namespace MyApp.Application.Commands.IngestBillingRecord;

public sealed class IngestBillingRecordHandler
    : IRequestHandler<IngestBillingRecordCommand, IngestionResponseDto>
{
    private readonly IBillingRepository      _repository;
    private readonly IBillingNormalizerFactory _normalizers;
    private readonly ILogger<IngestBillingRecordHandler> _logger;

    public IngestBillingRecordHandler(
        IBillingRepository        repository,
        IBillingNormalizerFactory normalizers,
        ILogger<IngestBillingRecordHandler> logger)
    {
        _repository  = repository;
        _normalizers = normalizers;
        _logger      = logger;
    }

    public async Task<IngestionResponseDto> Handle(
        IngestBillingRecordCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<CloudProvider>(request.Provider, ignoreCase: true, out var provider))
            throw new ArgumentException($"Unknown cloud provider: '{request.Provider}'.");

        _logger.LogInformation(
            "Ingesting billing record. Provider={Provider} AccountId={AccountId} CorrelationId={CorrelationId}",
            provider, request.AccountId, request.CorrelationId);

        var normalizer = _normalizers.GetNormalizer(provider);
        var record     = await normalizer.NormalizeAsync(
                             request.RawPayload, request.AccountId, request.CorrelationId, ct);

        await _repository.AddAsync(record, ct);
        record.MarkNormalized();
        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation("Billing record ingested successfully. RecordId={RecordId}", record.Id);

        return new IngestionResponseDto
        {
            RecordId      = record.Id,
            Status        = record.Status.ToString(),
            Provider      = provider.ToString(),
            AccountId     = request.AccountId,
            CorrelationId = request.CorrelationId
        };
    }
}
