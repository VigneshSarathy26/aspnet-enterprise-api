namespace MyApp.Domain.Common;

/// <summary>
/// Marker interface for all domain events.
/// Application layer wraps these in MediatR INotification for dispatch.
/// </summary>
public interface IDomainEvent { }
