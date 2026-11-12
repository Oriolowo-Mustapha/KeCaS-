namespace kecas.application.network.ListRoutes;

internal sealed class ListRoutesQueryHandler(
    IRouteRepository routeRepository,
    IStopRepository stopRepository)
    : IQueryHandler<ListRoutesQuery, ListRoutesResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<ListRoutesResponse>> Handle(
        ListRoutesQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var routes = await routeRepository.ListAsync(query.Category, query.IsActive, cancellationToken);
        var total = await routeRepository.CountAsync(query.Category, query.IsActive, cancellationToken);

        var stopIds = routes
            .SelectMany(r => r.Stops.Select(s => s.StopId))
            .Distinct()
            .ToList();
        var stopsById = (await stopRepository.ListByIdsAsync(stopIds, cancellationToken))
            .ToDictionary(s => s.Id);

        var items = routes
            .OrderBy(r => r.Code, StringComparer.OrdinalIgnoreCase)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => ToListItem(r, stopsById))
            .ToList();

        return Result.Success(new ListRoutesResponse(items, page, pageSize, total));
    }

    internal static RouteListItem ToListItem(
        kecas.domain.network.Route route,
        IReadOnlyDictionary<Guid, kecas.domain.network.Stop> stopsById) =>
        new(
            route.Id,
            route.Code,
            route.Name,
            route.Category.ToString().ToUpperInvariant(),
            route.Description,
            route.IsActive,
            route.Stops
                .OrderBy(s => s.Seq)
                .Select(s => new RouteStopItem(
                    stopsById.TryGetValue(s.StopId, out var stop) ? stop.Code : string.Empty,
                    stopsById.TryGetValue(s.StopId, out var stopName) ? stopName.Name : string.Empty,
                    s.Seq,
                    s.MinsFromStart))
                .ToList());
}
