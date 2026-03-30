using MediatR;
using MyApp.Application.DTOs;
using MyApp.Domain.Enums;

namespace MyApp.Application.Queries.GetBillingRecords;

public sealed record GetBillingRecordsQuery(
    string?        AccountId,
    CloudProvider? Provider,
    DateTime?      From,
    DateTime?      To,
    BillingStatus? Status,
    int            Page     = 1,
    int            PageSize = 50
) : IRequest<PagedResult<BillingRecordDto>>;
