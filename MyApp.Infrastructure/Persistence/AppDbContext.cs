using MediatR;
using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Common;
using MyApp.Domain.Entities;

namespace MyApp.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly IMediator _mediator;

    public AppDbContext(DbContextOptions<AppDbContext> options, IMediator mediator)
        : base(options)
    {
        _mediator = mediator;
    }

    public DbSet<BillingRecord> BillingRecords => Set<BillingRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Overrides SaveChangesAsync to dispatch domain events AFTER the transaction commits,
    /// ensuring events are only published when persistence succeeded.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var result = await base.SaveChangesAsync(ct);
        await DispatchDomainEventsAsync(ct);
        return result;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        var entities = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var events = entities.SelectMany(e => e.DomainEvents).ToList();
        entities.ForEach(e => e.ClearDomainEvents());

        foreach (var @event in events)
        {
            // Wrap the domain event in an MediatR INotification so Domain stays framework-free
            var notificationType  = typeof(DomainEventNotification<>).MakeGenericType(@event.GetType());
            var notification      = (MediatR.INotification)Activator.CreateInstance(notificationType, @event)!;
            await _mediator.Publish(notification, ct);
        }
    }
}
