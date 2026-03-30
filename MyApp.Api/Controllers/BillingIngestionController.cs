using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Commands.IngestBillingBatch;
using MyApp.Application.Commands.IngestBillingRecord;
using MyApp.Application.DTOs;

namespace MyApp.Api.Controllers;

/// <summary>Handles cloud billing telemetry ingestion — single record and batch.</summary>
[ApiController]
[Route("api/v1/billing")]
[Produces("application/json")]
public sealed class BillingIngestionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BillingIngestionController> _logger;

    public BillingIngestionController(IMediator mediator, ILogger<BillingIngestionController> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    /// <summary>Ingest a single cloud billing record.</summary>
    /// <response code="201">Record successfully ingested and normalized.</response>
    /// <response code="400">Invalid request payload or unknown provider.</response>
    [HttpPost("ingest")]
    [ProducesResponseType(typeof(IngestionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ingest(
        [FromBody] IngestionRequestDto request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                         ?? request.CorrelationId;

        var command = new IngestBillingRecordCommand(
            request.Provider,
            request.AccountId,
            request.RawPayload,
            correlationId);

        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(Ingest), new { id = result.RecordId }, result);
    }

    /// <summary>Ingest a batch of cloud billing records (up to 1000).</summary>
    /// <response code="202">Batch accepted for processing.</response>
    /// <response code="400">Invalid request payload.</response>
    [HttpPost("ingest/batch")]
    [ProducesResponseType(typeof(BatchIngestionResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestBatch(
        [FromBody] BatchIngestionRequestDto request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                         ?? request.CorrelationId;

        var command = new IngestBillingBatchCommand(
            request.Provider,
            request.AccountId,
            request.RawPayloads,
            correlationId);

        var result = await _mediator.Send(command, ct);
        return Accepted(result);
    }
}
