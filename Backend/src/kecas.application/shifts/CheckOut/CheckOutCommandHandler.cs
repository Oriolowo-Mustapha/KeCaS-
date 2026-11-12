namespace kecas.application.shifts.CheckOut;

using System.Text.Json;
using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;
using kecas.domain.fleet;

internal sealed class CheckOutCommandHandler(
    IDriverRepository driverRepository,
    IShiftRepository shiftRepository,
    IAuditWriter auditWriter,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CheckOutCommand>
{
    private const string CheckInOnBehalfPermission = "drivers:checkin";

    public async Task<Result> Handle(CheckOutCommand command, CancellationToken cancellationToken)
    {
        var shift = await shiftRepository.GetByIdAsync(command.ShiftId, cancellationToken);
        if (shift is null)
        {
            return Result.Failure(ShiftErrors.NotFound);
        }

        if (!command.ActorPermissions.Contains(CheckInOnBehalfPermission))
        {
            // Own-scope check-out: the shift must belong to the signed-in driver.
            var driver = await driverRepository.GetByUserIdAsync(command.ActorId, cancellationToken);
            if (driver is null || driver.Id != shift.DriverId)
            {
                return Result.Failure(AuthErrors.PermissionDenied);
            }
        }

        var result = shift.CheckOut(dateTimeProvider.UtcNow, ShiftClosedBy.User);
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "shift.checked_out",
            "shift",
            shift.Id.ToString(),
            before: JsonSerializer.Serialize(new { endedAt = (DateTime?)null }),
            after: JsonSerializer.Serialize(new { endedAt = shift.EndedAt }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
