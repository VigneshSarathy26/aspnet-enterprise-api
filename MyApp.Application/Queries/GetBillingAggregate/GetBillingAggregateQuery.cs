using MediatR;
using MyApp.Application.DTOs;
using MyApp.Domain.Enums;

namespace MyApp.Application.Queries.GetBillingAggregate;

public sealed record GetBillingAggregateQuery(
    string        AccountId,
    CloudProvider? Provider,
    DateTime      From,
    DateTime      To
) : IRequest<BillingAggregateDto>;
