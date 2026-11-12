namespace kecas.application.network.ListStops;

internal sealed class ListStopsQueryHandler(
    IZoneRepository zoneRepository,
    IStopRepository stopRepository)
    : IQueryHandler<ListStopsQuery, ListStopsResponse>
{
    public async Task<Result<ListStopsResponse>> Handle(
        ListStopsQuery query,
        CancellationToken cancellationToken)
    {
        var zones = await zoneRepository.ListAsync(cancellationToken);
        var zoneCodeById = zones.ToDictionary(z => z.Id, z => z.Code);

        if (query.ZoneId is { } zoneId && !zoneCodeById.ContainsKey(zoneId))
        {
            return Result.Failure<ListStopsResponse>(NetworkErrors.ZoneNotFound);
        }

        var stops = await stopRepository.ListAsync(query.ZoneId, cancellationToken);

        var items = stops
            .OrderBy(s => s.Code, StringComparer.OrdinalIgnoreCase)
            .Select(s => new StopListItem(
                s.Id,
                s.Code,
                s.Name,
                s.ZoneId,
                zoneCodeById.TryGetValue(s.ZoneId, out var zoneCode) ? zoneCode : string.Empty,
                s.IsRank))
            .ToList();

        return Result.Success(new ListStopsResponse(items, items.Count));
    }
}
