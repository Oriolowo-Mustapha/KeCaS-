namespace kecas.application.shifts.CheckIn;

using System.Text.Json;
using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;
using kecas.application.auth.CurrentUser;
using kecas.domain.fleet;

internal sealed class CheckInCommandHandler(
    IDriverRepository driverRepository,
    IVehicleRepository vehicleRepository,
    IStopRepository stopRepository,
    IShiftRepository shiftRepository,
    IAuditWriter auditWriter,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CheckInCommand, Guid>
{
    private const string CheckInOnBehalfPermission = "drivers:checkin";

    public async Task<Result<Guid>> Handle(CheckInCommand command, CancellationToken cancellationToken)
    {
        Driver? driver;

        if (command.DriverId is { } driverId && driverId != Guid.Empty)
        {
            // Acting on behalf of a driver needs drivers:checkin (§8.1).
            if (!command.ActorPermissions.Contains(CheckInOnBehalfPermission))
            {
                return Result.Failure<Guid>(AuthErrors.PermissionDenied);
            }

            driver = await driverRepository.GetByIdAsync(driverId, cancellationToken);
        }
        else
        {
            // Self check-in: the signed-in user must have a linked driver record.
            driver = await driverRepository.GetByUserIdAsync(command.ActorId, cancellationToken);
            if (driver is null)
            {
                return Result.Failure<Guid>(DriverErrors.NotFound);
            }
        }

        if (driver is null)
        {
            return Result.Failure<Guid>(DriverErrors.NotFound);
        }

        var vehicle = await vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return Result.Failure<Guid>(VehicleErrors.NotFound);
        }

        if (command.RankStopId is { } rankStopId && rankStopId != Guid.Empty
            && await stopRepository.GetByIdAsync(rankStopId, cancellationToken) is null)
        {
            return Result.Failure<Guid>(NetworkErrors.StopNotFound);
        }

        // Friendly pre-checks; the partial unique indexes are the final guard.
        if (await shiftRepository.FindOpenByDriverAsync(driver.Id, cancellationToken) is not null)
        {
            return Result.Failure<Guid>(ShiftErrors.AlreadyOpen);
        }

        if (await shiftRepository.FindOpenByVehicleAsync(vehicle.Id, cancellationToken) is not null)
        {
            return Result.Failure<Guid>(ShiftErrors.VehicleAlreadyOnShift);
        }

        // §7.2 guards live in the domain: driver ACTIVE + licence valid,
        // vehicle assignable.
        var shiftResult = Shift.CheckIn(driver, vehicle, dateTimeProvider.UtcNow, command.RankStopId);
        if (shiftResult.IsFailure)
        {
            return Result.Failure<Guid>(shiftResult.Error);
        }

        shiftRepository.Add(shiftResult.Value);

        auditWriter.Append(
            command.ActorId,
            "shift.checked_in",
            "shift",
            shiftResult.Value.Id.ToString(),
            after: JsonSerializer.Serialize(new
            {
                driverId = driver.Id,
                vehicleId = vehicle.Id,
                rankStopId = shiftResult.Value.RankStopId,
                startedAt = shiftResult.Value.StartedAt,
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return shiftResult.Value.Id;
    }
}
