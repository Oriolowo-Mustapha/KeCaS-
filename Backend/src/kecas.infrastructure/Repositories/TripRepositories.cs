namespace kecas.infrastructure.Repositories;

using kecas.domain.trips;
using Microsoft.EntityFrameworkCore;

internal sealed class TripRepository(ApplicationDbContext context) : ITripRepository
{
    public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Trips.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Trip?> GetByCodeAsync(string tripCode, CancellationToken cancellationToken = default) =>
        context.Trips.FirstOrDefaultAsync(t => t.TripCode == tripCode.ToUpperInvariant(), cancellationToken);

    public async Task<IReadOnlyList<Trip>> ListAsync(
        int page,
        int pageSize,
        TripStatus? status = null,
        Guid? routeId = null,
        Guid? vehicleId = null,
        Guid? driverId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = Filter(status, routeId, vehicleId, driverId, from, to);

        var trips = await query
            .AsNoTracking()
            .OrderByDescending(t => t.StartsAt)
            .ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return trips;
    }

    public Task<int> CountAsync(
        TripStatus? status = null,
        Guid? routeId = null,
        Guid? vehicleId = null,
        Guid? driverId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default) =>
        Filter(status, routeId, vehicleId, driverId, from, to).CountAsync(cancellationToken);

    public Task<Trip?> FindOverlappingAsync(
        Guid? driverId,
        Guid? vehicleId,
        DateTime startsAt,
        DateTime endsAt,
        Guid? excludeTripId = null,
        CancellationToken cancellationToken = default)
    {
        // BR-03: one live trip at a time per driver and per vehicle. Live =
        // SCHEDULED or IN_PROGRESS (cancel frees the slot). IsLive is a
        // computed property EF cannot translate, so expand it here.
        return context.Trips
            .Where(t => t.Status == TripStatus.Scheduled || t.Status == TripStatus.InProgress)
            .Where(t => excludeTripId == null || t.Id != excludeTripId.Value)
            .Where(t => t.StartsAt < endsAt && t.EndsAt > startsAt)
            .Where(t =>
                (driverId != null && t.DriverId == driverId.Value)
                || (vehicleId != null && t.VehicleId == vehicleId.Value))
            .OrderBy(t => t.StartsAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Trip>> ListLiveForAvailabilityAsync(
        DateTime nowUtc,
        DateTime horizonUtc,
        CancellationToken cancellationToken = default)
    {
        // Rule 3 (§5.1): IN_PROGRESS, or SCHEDULED starting within the horizon.
        var trips = await context.Trips
            .AsNoTracking()
            .Where(t => t.Status == TripStatus.InProgress
                || (t.Status == TripStatus.Scheduled && t.StartsAt <= horizonUtc && t.StartsAt > nowUtc))
            .ToListAsync(cancellationToken);

        return trips;
    }

    public async Task<bool> TryLockAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        // §7.4 step 4 (BR-09): lock the trip row so the confirmed-seat sum and
        // the confirm update are serialized per trip. Runs inside the ambient
        // transaction begun by the caller (IUnitOfWork.BeginTransactionAsync).
        var locked = await context.Database
            .SqlQuery<Guid?>($"SELECT id AS \"Value\" FROM trips WHERE id = {tripId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);

        return locked.HasValue;
    }

    public void Add(Trip trip) => context.Trips.Add(trip);

    private IQueryable<Trip> Filter(
        TripStatus? status,
        Guid? routeId,
        Guid? vehicleId,
        Guid? driverId,
        DateTime? from,
        DateTime? to)
    {
        var query = context.Trips.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (routeId.HasValue)
        {
            query = query.Where(t => t.RouteId == routeId.Value);
        }

        if (vehicleId.HasValue)
        {
            query = query.Where(t => t.VehicleId == vehicleId.Value);
        }

        if (driverId.HasValue)
        {
            query = query.Where(t => t.DriverId == driverId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(t => t.StartsAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(t => t.StartsAt <= to.Value);
        }

        return query;
    }
}
