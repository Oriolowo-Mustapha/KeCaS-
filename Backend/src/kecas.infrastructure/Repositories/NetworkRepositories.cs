namespace kecas.infrastructure.Repositories;

using kecas.domain.network;
using Microsoft.EntityFrameworkCore;

internal sealed class ZoneRepository(ApplicationDbContext context) : IZoneRepository
{
    public Task<Zone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Zones.FirstOrDefaultAsync(z => z.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Zone>> ListAsync(CancellationToken cancellationToken = default)
    {
        var zones = await context.Zones
            .AsNoTracking()
            .OrderBy(z => z.Code)
            .ToListAsync(cancellationToken);

        return zones;
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        context.Zones.AnyAsync(z => z.Code == code.Trim().ToUpperInvariant(), cancellationToken);

    public void Add(Zone zone) => context.Zones.Add(zone);
}

internal sealed class StopRepository(ApplicationDbContext context) : IStopRepository
{
    public Task<Stop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Stops.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Stop>> ListAsync(
        Guid? zoneId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Stops.AsQueryable();

        if (zoneId.HasValue)
        {
            query = query.Where(s => s.ZoneId == zoneId.Value);
        }

        var stops = await query
            .AsNoTracking()
            .OrderBy(s => s.Code)
            .ToListAsync(cancellationToken);

        return stops;
    }

    public async Task<IReadOnlyList<Stop>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var stops = await context.Stops
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(cancellationToken);

        return stops;
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        context.Stops.AnyAsync(s => s.Code == code.Trim().ToUpperInvariant(), cancellationToken);

    public void Add(Stop stop) => context.Stops.Add(stop);
}

internal sealed class RouteRepository(ApplicationDbContext context) : IRouteRepository
{
    public Task<Route?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Routes.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Route>> ListAsync(
        RouteCategory? category = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = Filter(category, isActive);

        var routes = await query
            .AsNoTracking()
            .OrderBy(r => r.Code)
            .ToListAsync(cancellationToken);

        return routes;
    }

    public Task<int> CountAsync(
        RouteCategory? category = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Filter(category, isActive).CountAsync(cancellationToken);

    private IQueryable<Route> Filter(RouteCategory? category, bool? isActive)
    {
        var query = context.Routes.AsQueryable();

        if (category.HasValue)
        {
            query = query.Where(r => r.Category == category.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        return query;
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        context.Routes.AnyAsync(r => r.Code == code.Trim().ToUpperInvariant(), cancellationToken);

    public void Add(Route route) => context.Routes.Add(route);
}
