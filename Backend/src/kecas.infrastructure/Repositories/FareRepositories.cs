namespace kecas.infrastructure.Repositories;

using kecas.domain.fares;
using kecas.domain.fleet;
using Microsoft.EntityFrameworkCore;

internal sealed class FareRepository(ApplicationDbContext context) : IFareRepository
{
    public Task<Fare?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Fares.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Fare>> ListAsync(
        int page,
        int pageSize,
        FareStatus? status = null,
        Guid? routeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = Filter(status, routeId);

        var fares = await query
            .AsNoTracking()
            .OrderByDescending(f => f.EffectiveFrom)
            .ThenBy(f => f.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return fares;
    }

    public Task<int> CountAsync(
        FareStatus? status = null,
        Guid? routeId = null,
        CancellationToken cancellationToken = default) =>
        Filter(status, routeId).CountAsync(cancellationToken);

    public Task<Fare?> FindPublishedForScopeAsync(
        Guid? routeId,
        Guid? fromZoneId,
        Guid? toZoneId,
        CancellationToken cancellationToken = default) =>
        context.Fares
            .FirstOrDefaultAsync(
                f => f.Status == FareStatus.Published
                    && f.RouteId == routeId
                    && f.FromZoneId == fromZoneId
                    && f.ToZoneId == toZoneId,
                cancellationToken);

    public async Task<IReadOnlyList<Fare>> ListPublishedAsync(CancellationToken cancellationToken = default)
    {
        var fares = await context.Fares
            .AsNoTracking()
            .Where(f => f.Status == FareStatus.Published)
            .OrderBy(f => f.Id)
            .ToListAsync(cancellationToken);

        return fares;
    }

    private IQueryable<Fare> Filter(FareStatus? status, Guid? routeId)
    {
        var query = context.Fares.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        if (routeId.HasValue)
        {
            query = query.Where(f => f.RouteId == routeId.Value);
        }

        return query;
    }

    public void Add(Fare fare) => context.Fares.Add(fare);
}

internal sealed class ShiftRepository(ApplicationDbContext context) : IShiftRepository
{
    public Task<Shift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Shifts.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Shift>> ListOpenAsync(CancellationToken cancellationToken = default)
    {
        var shifts = await context.Shifts
            .AsNoTracking()
            .Where(s => s.EndedAt == null)
            .OrderBy(s => s.StartedAt)
            .ToListAsync(cancellationToken);

        return shifts;
    }

    public async Task<IReadOnlyList<Shift>> ListAsync(
        int page,
        int pageSize,
        bool? openOnly = null,
        Guid? driverId = null,
        Guid? vehicleId = null,
        CancellationToken cancellationToken = default)
    {
        var query = Filter(openOnly, driverId, vehicleId);

        var shifts = await query
            .AsNoTracking()
            .OrderByDescending(s => s.StartedAt)
            .ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return shifts;
    }

    public Task<int> CountAsync(
        bool? openOnly = null,
        Guid? driverId = null,
        Guid? vehicleId = null,
        CancellationToken cancellationToken = default) =>
        Filter(openOnly, driverId, vehicleId).CountAsync(cancellationToken);

    public Task<Shift?> FindOpenByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        context.Shifts
            .FirstOrDefaultAsync(s => s.VehicleId == vehicleId && s.EndedAt == null, cancellationToken);

    public Task<Shift?> FindOpenByDriverAsync(Guid driverId, CancellationToken cancellationToken = default) =>
        context.Shifts
            .FirstOrDefaultAsync(s => s.DriverId == driverId && s.EndedAt == null, cancellationToken);

    public async Task<IReadOnlyList<Shift>> ListStaleOpenAsync(
        DateTime cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        // Open shifts that started before the cutoff — the stale-shift guard
        // (§5.1) closes them with ClosedBy = Auto.
        var shifts = await context.Shifts
            .Where(s => s.EndedAt == null && s.StartedAt < cutoffUtc)
            .OrderBy(s => s.StartedAt)
            .ToListAsync(cancellationToken);

        return shifts;
    }

    private IQueryable<Shift> Filter(bool? openOnly, Guid? driverId, Guid? vehicleId)
    {
        var query = context.Shifts.AsQueryable();

        if (openOnly == true)
        {
            query = query.Where(s => s.EndedAt == null);
        }

        if (driverId.HasValue)
        {
            query = query.Where(s => s.DriverId == driverId.Value);
        }

        if (vehicleId.HasValue)
        {
            query = query.Where(s => s.VehicleId == vehicleId.Value);
        }

        return query;
    }

    public void Add(Shift shift) => context.Shifts.Add(shift);
}
