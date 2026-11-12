namespace kecas.application.fares.ListFares;

internal sealed class ListFaresQueryHandler(
    IFareRepository fareRepository,
    IRouteRepository routeRepository,
    IZoneRepository zoneRepository)
    : IQueryHandler<ListFaresQuery, ListFaresResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<ListFaresResponse>> Handle(
        ListFaresQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var fares = await fareRepository.ListAsync(page, pageSize, query.Status, query.RouteId, cancellationToken);
        var total = await fareRepository.CountAsync(query.Status, query.RouteId, cancellationToken);

        var (routeById, zoneCodeById) = await LoadScopeDataAsync(fares, routeRepository, zoneRepository, cancellationToken);

        var items = fares
            .Select(f => FareMapper.ToListItem(f, routeById, zoneCodeById))
            .ToList();

        return Result.Success(new ListFaresResponse(items, page, pageSize, total));
    }

    internal static async Task<(IReadOnlyDictionary<Guid, kecas.domain.network.Route> RouteById, IReadOnlyDictionary<Guid, string> ZoneCodes)>
        LoadScopeDataAsync(
            IReadOnlyList<kecas.domain.fares.Fare> fares,
            IRouteRepository routeRepository,
            IZoneRepository zoneRepository,
            CancellationToken cancellationToken)
    {
        var routeIds = fares.Where(f => f.RouteId is not null).Select(f => f.RouteId!.Value).Distinct().ToList();
        var zoneIds = fares
            .Where(f => f.FromZoneId is not null)
            .SelectMany(f => new[] { f.FromZoneId!.Value, f.ToZoneId!.Value })
            .Distinct()
            .ToList();

        var routeById = routeIds.Count == 0
            ? new Dictionary<Guid, kecas.domain.network.Route>()
            : (await routeRepository.ListAsync(cancellationToken: cancellationToken))
                .Where(r => routeIds.Contains(r.Id))
                .ToDictionary(r => r.Id);

        var zoneCodes = zoneIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await zoneRepository.ListAsync(cancellationToken))
                .Where(z => zoneIds.Contains(z.Id))
                .ToDictionary(z => z.Id, z => z.Code);

        return (routeById, zoneCodes);
    }
}
