namespace kecas.application.shifts.ListShifts;

internal sealed class ListShiftsQueryHandler(
    IShiftRepository shiftRepository,
    IStopRepository stopRepository)
    : IQueryHandler<ListShiftsQuery, ListShiftsResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<ListShiftsResponse>> Handle(
        ListShiftsQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var shifts = await shiftRepository.ListAsync(page, pageSize, query.OpenOnly, query.DriverId, query.VehicleId, cancellationToken);
        var total = await shiftRepository.CountAsync(query.OpenOnly, query.DriverId, query.VehicleId, cancellationToken);

        var rankStopIds = shifts
            .Where(s => s.RankStopId is not null)
            .Select(s => s.RankStopId!.Value)
            .Distinct()
            .ToList();
        var stopsById = rankStopIds.Count == 0
            ? new Dictionary<Guid, kecas.domain.network.Stop>()
            : (await stopRepository.ListByIdsAsync(rankStopIds, cancellationToken))
                .ToDictionary(s => s.Id);

        var items = shifts
            .OrderByDescending(s => s.StartedAt)
            .Select(s => new ShiftListItem(
                s.Id,
                s.DriverId,
                s.VehicleId,
                s.RankStopId,
                s.RankStopId is { } rankId && stopsById.TryGetValue(rankId, out var stop) ? stop.Code : null,
                s.StartedAt,
                s.EndedAt,
                s.ClosedBy?.ToString().ToUpperInvariant()))
            .ToList();

        return Result.Success(new ListShiftsResponse(items, page, pageSize, total));
    }
}
