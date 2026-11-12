namespace kecas.application.network.ListZones;

internal sealed class ListZonesQueryHandler(IZoneRepository zoneRepository)
    : IQueryHandler<ListZonesQuery, ListZonesResponse>
{
    public async Task<Result<ListZonesResponse>> Handle(
        ListZonesQuery query,
        CancellationToken cancellationToken)
    {
        var zones = await zoneRepository.ListAsync(cancellationToken);

        var items = zones
            .OrderBy(z => z.Code, StringComparer.OrdinalIgnoreCase)
            .Select(z => new ZoneListItem(z.Id, z.Code, z.Name))
            .ToList();

        return Result.Success(new ListZonesResponse(items, items.Count));
    }
}
