namespace kecas.application.trips.ListTrips;

using kecas.domain.network;
using kecas.domain.trips;

internal sealed class ListTripsQueryHandler(
    ITripRepository tripRepository,
    IRouteRepository routeRepository,
    IVehicleRepository vehicleRepository)
    : IQueryHandler<ListTripsQuery, ListTripsResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<ListTripsResponse>> Handle(
        ListTripsQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var trips = await tripRepository.ListAsync(
            page, pageSize, query.Status, query.RouteId, query.VehicleId, query.DriverId,
            query.From, query.To, cancellationToken);
        var total = await tripRepository.CountAsync(
            query.Status, query.RouteId, query.VehicleId, query.DriverId, query.From, query.To, cancellationToken);

        var routeIds = trips.Select(t => t.RouteId).Distinct().ToList();
        var routeById = routeIds.Count == 0
            ? new Dictionary<Guid, kecas.domain.network.Route>()
            : (await routeRepository.ListAsync(cancellationToken: cancellationToken))
                .Where(r => routeIds.Contains(r.Id))
                .ToDictionary(r => r.Id);

        var vehicleIds = trips.Where(t => t.VehicleId is not null).Select(t => t.VehicleId!.Value).Distinct().ToList();
        var vehicleById = vehicleIds.Count == 0
            ? new Dictionary<Guid, kecas.domain.fleet.Vehicle>()
            : (await vehicleRepository.ListByIdsAsync(vehicleIds, cancellationToken))
                .ToDictionary(v => v.Id);

        var items = trips
            .Select(t => TripMapper.ToListItem(t, routeById, vehicleById))
            .ToList();

        return Result.Success(new ListTripsResponse(items, page, pageSize, total));
    }
}
