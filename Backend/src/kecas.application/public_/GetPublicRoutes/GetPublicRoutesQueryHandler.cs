namespace kecas.application.public_.GetPublicRoutes;

internal sealed class GetPublicRoutesQueryHandler(
    IRouteRepository routeRepository,
    IStopRepository stopRepository)
    : IQueryHandler<GetPublicRoutesQuery, PublicRoutesResponse>
{
    public async Task<Result<PublicRoutesResponse>> Handle(
        GetPublicRoutesQuery query,
        CancellationToken cancellationToken)
    {
        var routes = await routeRepository.ListAsync(isActive: true, cancellationToken: cancellationToken);

        var stopIds = routes
            .SelectMany(r => r.Stops.Select(s => s.StopId))
            .Distinct()
            .ToList();
        var stopsById = (await stopRepository.ListByIdsAsync(stopIds, cancellationToken))
            .ToDictionary(s => s.Id);

        var items = routes
            .OrderBy(r => r.Code, StringComparer.OrdinalIgnoreCase)
            .Select(r => new PublicRouteItem(
                r.Code,
                r.Name,
                r.Category.ToString().ToUpperInvariant(),
                r.Description,
                r.Stops
                    .OrderBy(s => s.Seq)
                    .Select(s => new PublicRouteStopItem(
                        stopsById.TryGetValue(s.StopId, out var stop) ? stop.Code : string.Empty,
                        stopsById.TryGetValue(s.StopId, out var stopName) ? stopName.Name : string.Empty,
                        s.Seq,
                        s.MinsFromStart))
                    .ToList()))
            .ToList();

        return Result.Success(new PublicRoutesResponse(items));
    }
}
