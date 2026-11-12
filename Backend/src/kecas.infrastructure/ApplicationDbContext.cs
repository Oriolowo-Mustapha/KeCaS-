namespace kecas.infrastructure;

using kecas.application.abstractions.messaging;
using kecas.domain.audit;
using kecas.domain.bookings;
using kecas.domain.fares;
using kecas.domain.feedback;
using kecas.domain.fleet;
using kecas.domain.network;
using kecas.domain.notices;
using kecas.domain.payments;
using kecas.domain.trips;
using kecas.domain.users;
using MediatR;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The one PostgreSQL datastore (design doc §3.1). Implements IUnitOfWork so
/// handlers commit without touching EF Core. Domain events raised by
/// aggregates are published after a successful save (audit rows will be
/// written in the same transaction once the audit module lands — DD-11).
/// </summary>
public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IMediator mediator)
    : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<Operator> Operators => Set<Operator>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<Driver> Drivers => Set<Driver>();

    public DbSet<Shift> Shifts => Set<Shift>();

    public DbSet<Zone> Zones => Set<Zone>();

    public DbSet<Stop> Stops => Set<Stop>();

    public DbSet<Route> Routes => Set<Route>();

    public DbSet<RouteStop> RouteStops => Set<RouteStop>();

    public DbSet<Fare> Fares => Set<Fare>();

    public DbSet<Trip> Trips => Set<Trip>();

    public DbSet<kecas.domain.notices.Notice> Notices => Set<kecas.domain.notices.Notice>();

    public DbSet<kecas.domain.bookings.Booking> Bookings => Set<kecas.domain.bookings.Booking>();

    public DbSet<kecas.domain.payments.Payment> Payments => Set<kecas.domain.payments.Payment>();

    public DbSet<kecas.domain.feedback.Feedback> Feedback => Set<kecas.domain.feedback.Feedback>();

    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entitiesWithEvents = ChangeTracker
            .Entries<Entity>()
            .Where(entry => entry.Entity.GetDomainEvents().Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(entity => entity.GetDomainEvents())
            .ToList();

        entitiesWithEvents.ForEach(entity => entity.ClearDomainEvents());

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent, cancellationToken);
        }

        return result;
    }

    /// <summary>Explicit transaction support for the §5.3 supersede-and-publish save.</summary>
    public Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Database.BeginTransactionAsync(cancellationToken);

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) =>
        Database.CommitTransactionAsync(cancellationToken);

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) =>
        Database.RollbackTransactionAsync(cancellationToken);
}
