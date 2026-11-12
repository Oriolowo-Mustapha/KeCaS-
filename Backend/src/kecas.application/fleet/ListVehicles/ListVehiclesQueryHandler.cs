namespace kecas.application.fleet.ListVehicles;

using kecas.application.abstractions.data;

internal sealed class ListVehiclesQueryHandler(IVehicleRepository vehicleRepository)
    : IQueryHandler<ListVehiclesQuery, ListVehiclesResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<ListVehiclesResponse>> Handle(
        ListVehiclesQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var vehicles = await vehicleRepository.ListAsync(
            page, pageSize, query.Category, query.Status, query.OperatorId, cancellationToken);
        var total = await vehicleRepository.CountAsync(
            query.Category, query.Status, query.OperatorId, cancellationToken);

        var items = vehicles.Select(v => new VehicleListItem(
            v.Id,
            v.Code.Value,
            v.PlateNumber.Value,
            v.Category,
            v.Capacity,
            v.Status.ToString(),
            v.StatusChangedAt,
            v.OperatorId)).ToList();

        return Result.Success(new ListVehiclesResponse(items, page, pageSize, total));
    }
}
