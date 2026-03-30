using MediatR;
using MyApp.Application.DTOs;

namespace MyApp.Application.Commands.IngestBillingBatch;

public sealed record IngestBillingBatchCommand(
    string              Provider,
    string              AccountId,
    IEnumerable<string> RawPayloads,
    string?             CorrelationId
) : IRequest<BatchIngestionResponseDto>;
