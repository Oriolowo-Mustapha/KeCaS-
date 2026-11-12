namespace kecas.application.fleet.GetVehicle;

using kecas.application.abstractions.data;

internal sealed class GetVehicleQueryHandler(IVehicleRepository vehicleRepository)
    : IQueryHandler<GetVehicleQuery, VehicleListItem>
{
    public async Task<Result<VehicleListItem>> Handle(
        GetVehicleQuery query,
        CancellationToken cancellationToken)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(query.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return Result.Failure<VehicleListItem>(VehicleErrors.NotFound);
        }

        return Result.Success(new VehicleListItem(
            vehicle.Id,
            vehicle.Code.Value,
            vehicle.PlateNumber.Value,
            vehicle.Category,
            vehicle.Capacity,
            vehicle.Status.ToString(),
            vehicle.StatusChangedAt,
            vehicle.OperatorId));
    }
}
