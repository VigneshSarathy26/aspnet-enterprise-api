using MediatR;
using MyApp.Domain.Common;

namespace MyApp.Infrastructure.Persistence;

/// <summary>
/// Wraps a domain IDomainEvent in MediatR's INotification so the Infrastructure
/// layer can dispatch it without forcing MediatR on the Domain project.
/// </summary>
internal sealed record DomainEventNotification<T>(T Event) : INotification
    where T : IDomainEvent;
