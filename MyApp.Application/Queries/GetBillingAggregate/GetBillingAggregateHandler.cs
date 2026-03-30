using MediatR;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;

namespace MyApp.Application.Queries.GetBillingAggregate;

public sealed class GetBillingAggregateHandler
    : IRequestHandler<GetBillingAggregateQuery, BillingAggregateDto>
{
    private readonly IBillingRepository _repository;
    private readonly ICacheService      _cache;

    public GetBillingAggregateHandler(IBillingRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache      = cache;
    }

    public async Task<BillingAggregateDto> Handle(
        GetBillingAggregateQuery request, CancellationToken ct)
    {
        var cacheKey = $"agg:{request.AccountId}:{request.Provider}:{request.From:yyyyMMdd}:{request.To:yyyyMMdd}";
        var cached   = await _cache.GetAsync<BillingAggregateDto>(cacheKey, ct);
        if (cached is not null) return cached;

        var result = await _repository.ComputeAggregateAsync(
            request.AccountId, request.Provider, request.From, request.To, ct);

        var dto = new BillingAggregateDto
        {
            AccountId   = result.AccountId,
            Provider    = result.Provider,
            TotalCost   = result.TotalCost,
            Currency    = result.Currency,
            RecordCount = result.RecordCount,
            PeriodStart = result.PeriodStart,
            PeriodEnd   = result.PeriodEnd
        };

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), ct);
        return dto;
    }
}
