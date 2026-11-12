namespace kecas.application.public_.VerifyVehicle;

using kecas.domain.fleet;
using kecas.domain.users;

internal sealed class VerifyVehicleQueryHandler(
    IVehicleRepository vehicleRepository,
    IOperatorRepository operatorRepository,
    IDriverRepository driverRepository,
    IShiftRepository shiftRepository,
    IUserRepository userRepository)
    : IQueryHandler<VerifyVehicleQuery, VerifyVehicleResponse>
{
    public async Task<Result<VerifyVehicleResponse>> Handle(
        VerifyVehicleQuery query,
        CancellationToken cancellationToken)
    {
        var vehicle = await vehicleRepository.GetByPlateAsync(query.PlateNumber.Trim(), cancellationToken);
        if (vehicle is null)
        {
            return Result.Failure<VerifyVehicleResponse>(VehicleErrors.NotFound);
        }

        var operator_ = await operatorRepository.GetByIdAsync(vehicle.OperatorId, cancellationToken);

        // Driver first name comes from the open shift occupying the vehicle,
        // if any; otherwise the vehicle still verifies but has no driver shown.
        string? driverFirstName = null;
        var openShift = await shiftRepository.FindOpenByVehicleAsync(vehicle.Id, cancellationToken);
        if (openShift is not null)
        {
            var driver = await driverRepository.GetByIdAsync(openShift.DriverId, cancellationToken);
            if (driver?.UserId is { } userId)
            {
                var user = await userRepository.GetByIdAsync(userId, cancellationToken);
                var fullName = user?.FullName;
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    driverFirstName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                }
            }
        }

        return Result.Success(new VerifyVehicleResponse(
            vehicle.Code.Value,
            vehicle.PlateNumber.Value,
            vehicle.Category.ToString().ToUpperInvariant(),
            operator_?.Name ?? string.Empty,
            driverFirstName));
    }
}
