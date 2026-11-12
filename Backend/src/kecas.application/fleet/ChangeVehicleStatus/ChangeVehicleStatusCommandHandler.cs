namespace kecas.application.fleet.ChangeVehicleStatus;

using System.Text.Json;
using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;
using kecas.domain.fleet;

internal sealed class ChangeVehicleStatusCommandHandler(
    IVehicleRepository vehicleRepository,
    IShiftRepository shiftRepository,
    IAuditWriter auditWriter,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ChangeVehicleStatusCommand>
{
    public async Task<Result> Handle(ChangeVehicleStatusCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return Result.Failure(VehicleErrors.NotFound);
        }

        var before = JsonSerializer.Serialize(new { status = vehicle.Status.ToString() });

        var result = vehicle.ChangeStatus(command.Status, command.Reason);
        if (result.IsFailure)
        {
            return result;
        }

        // §5.3: a vehicle leaving ACTIVE cannot stay on duty — its open
        // shift is closed automatically (ClosedBy = Auto) with an audit row.
        Guid? closedShiftId = null;
        if (vehicle.Status != VehicleStatus.Active
            && await shiftRepository.FindOpenByVehicleAsync(vehicle.Id, cancellationToken) is { } openShift)
        {
            var closeResult = openShift.CheckOut(dateTimeProvider.UtcNow, ShiftClosedBy.Auto);
            if (closeResult.IsFailure)
            {
                return closeResult;
            }

            auditWriter.Append(
                command.ActorId,
                "shift.closed_auto",
                "shift",
                openShift.Id.ToString(),
                before: JsonSerializer.Serialize(new { endedAt = (DateTime?)null }),
                after: JsonSerializer.Serialize(new
                {
                    endedAt = openShift.EndedAt,
                    reason = $"Vehicle {vehicle.Code.Value} status changed to {vehicle.Status}",
                }));

            closedShiftId = openShift.Id;
        }

        auditWriter.Append(
            command.ActorId,
            "vehicle.status_changed",
            "vehicle",
            vehicle.Id.ToString(),
            before,
            JsonSerializer.Serialize(new
            {
                status = vehicle.Status.ToString(),
                reason = command.Reason,
                closedShiftId,
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
