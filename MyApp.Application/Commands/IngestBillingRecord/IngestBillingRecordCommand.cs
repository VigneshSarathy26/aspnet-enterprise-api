using MediatR;
using MyApp.Application.DTOs;

namespace MyApp.Application.Commands.IngestBillingRecord;

public sealed record IngestBillingRecordCommand(
    string  Provider,
    string  AccountId,
    string  RawPayload,
    string? CorrelationId
) : IRequest<IngestionResponseDto>;
