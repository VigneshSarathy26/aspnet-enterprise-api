using Mapster;
using MediatR;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;

namespace MyApp.Application.Queries.GetBillingRecords;

public sealed class GetBillingRecordsHandler
    : IRequestHandler<GetBillingRecordsQuery, PagedResult<BillingRecordDto>>
{
    private readonly IBillingRepository _repository;

    public GetBillingRecordsHandler(IBillingRepository repository)
        => _repository = repository;

    public async Task<PagedResult<BillingRecordDto>> Handle(
        GetBillingRecordsQuery request, CancellationToken ct)
    {
        var filter = new BillingRecordsFilter
        {
            AccountId = request.AccountId,
            Provider  = request.Provider,
            From      = request.From,
            To        = request.To,
            Status    = request.Status,
            Page      = request.Page,
            PageSize  = Math.Clamp(request.PageSize, 1, 200)
        };

        var (records, total) = await _repository.GetPagedAsync(filter, ct);

        // Mapster projection — uses registered TypeAdapterConfig
        var dtos = records.Adapt<IReadOnlyList<BillingRecordDto>>();

        return new PagedResult<BillingRecordDto>
        {
            Items      = dtos,
            TotalCount = total,
            Page       = filter.Page,
            PageSize   = filter.PageSize
        };
    }
}
