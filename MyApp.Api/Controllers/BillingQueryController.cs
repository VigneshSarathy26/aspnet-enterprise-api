using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.DTOs;
using MyApp.Application.Queries.GetBillingAggregate;
using MyApp.Application.Queries.GetBillingRecords;
using MyApp.Domain.Enums;

namespace MyApp.Api.Controllers;

/// <summary>Serves normalized billing telemetry records and cost aggregations.</summary>
[ApiController]
[Route("api/v1/billing")]
[Produces("application/json")]
public sealed class BillingQueryController : ControllerBase
{
    private readonly IMediator _mediator;

    public BillingQueryController(IMediator mediator) => _mediator = mediator;

    /// <summary>Query normalized billing records with optional filters and pagination.</summary>
    /// <param name="accountId">Filter by cloud account ID.</param>
    /// <param name="provider">Filter by cloud provider (AWS, Azure, GCP).</param>
    /// <param name="from">Filter records with period start >= this date.</param>
    /// <param name="to">Filter records with period end <= this date.</param>
    /// <param name="status">Filter by normalization status.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Records per page (max 200).</param>
    [HttpGet("records")]
    [ProducesResponseType(typeof(PagedResult<BillingRecordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecords(
        [FromQuery] string?        accountId,
        [FromQuery] CloudProvider? provider,
        [FromQuery] DateTime?      from,
        [FromQuery] DateTime?      to,
        [FromQuery] BillingStatus? status,
        [FromQuery] int            page     = 1,
        [FromQuery] int            pageSize = 50,
        CancellationToken ct = default)
    {
        var query  = new GetBillingRecordsQuery(accountId, provider, from, to, status, page, pageSize);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>Compute aggregated cost totals for an account over a time period.</summary>
    /// <param name="accountId">Target cloud account ID (required).</param>
    /// <param name="provider">Cloud provider filter (optional).</param>
    /// <param name="from">Period start date (required).</param>
    /// <param name="to">Period end date (required).</param>
    [HttpGet("aggregate")]
    [ProducesResponseType(typeof(BillingAggregateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAggregate(
        [FromQuery] string        accountId,
        [FromQuery] CloudProvider? provider,
        [FromQuery] DateTime       from,
        [FromQuery] DateTime       to,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            return BadRequest(new ProblemDetails { Title = "accountId is required." });

        if (from >= to)
            return BadRequest(new ProblemDetails { Title = "'from' must be before 'to'." });

        var query  = new GetBillingAggregateQuery(accountId, provider, from, to);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }
}
