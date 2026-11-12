namespace kecas.application.trips.ListMyTrips;

using kecas.application.abstractions.data;
using kecas.domain.fleet;
using kecas.domain.network;
using kecas.domain.trips;

internal sealed class ListMyTripsQueryHandler(
    IDriverRepository driverRepository,
    ITripRepository tripRepository,
    IRouteRepository routeRepository,
    IVehicleRepository vehicleRepository)
    : IQueryHandler<ListMyTripsQuery, ListTripsResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<ListTripsResponse>> Handle(
        ListMyTripsQuery query,
        CancellationToken cancellationToken)
    {
        var driver = await driverRepository.GetByUserIdAsync(query.ActorId, cancellationToken);
        if (driver is null)
        {
            return Result.Failure<ListTripsResponse>(AuthErrors.PermissionDenied);
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var trips = await tripRepository.ListAsync(
            page, pageSize, query.Status, driverId: driver.Id, cancellationToken: cancellationToken);
        var total = await tripRepository.CountAsync(
            query.Status, driverId: driver.Id, cancellationToken: cancellationToken);

        var routeIds = trips.Select(t => t.RouteId).Distinct().ToList();
        var routeById = routeIds.Count == 0
            ? new Dictionary<Guid, Route>()
            : (await routeRepository.ListAsync(cancellationToken: cancellationToken))
                .Where(r => routeIds.Contains(r.Id))
                .ToDictionary(r => r.Id);

        var vehicleIds = trips.Where(t => t.VehicleId is not null).Select(t => t.VehicleId!.Value).Distinct().ToList();
        var vehicleById = vehicleIds.Count == 0
            ? new Dictionary<Guid, Vehicle>()
            : (await vehicleRepository.ListByIdsAsync(vehicleIds, cancellationToken))
                .ToDictionary(v => v.Id);

        var items = trips
            .Select(t => TripMapper.ToListItem(t, routeById, vehicleById))
            .ToList();

        return Result.Success(new ListTripsResponse(items, page, pageSize, total));
    }
}
