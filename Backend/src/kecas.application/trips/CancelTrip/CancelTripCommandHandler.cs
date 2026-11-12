namespace kecas.application.trips.CancelTrip;

using System.Text.Json;
using kecas.application.abstractions.data;
using kecas.domain.trips;

internal sealed class CancelTripCommandHandler(
    ITripRepository tripRepository,
    IDriverRepository driverRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelTripCommand>
{
    private const string ManageTripsPermission = "trips:manage";

    public async Task<Result> Handle(CancelTripCommand command, CancellationToken cancellationToken)
    {
        var trip = await tripRepository.GetByIdAsync(command.TripId, cancellationToken);
        if (trip is null)
        {
            return Result.Failure(TripErrors.NotFound);
        }

        // trips:manage, or the assigned driver aborting their own trip.
        if (!command.ActorPermissions.Contains(ManageTripsPermission))
        {
            var ownDriver = await driverRepository.GetByUserIdAsync(command.ActorId, cancellationToken);
            if (ownDriver is null || ownDriver.Id != trip.DriverId)
            {
                return Result.Failure(AuthErrors.PermissionDenied);
            }
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Result.Failure(TripErrors.ReasonRequired);
        }

        var previousStatus = trip.Status;

        var result = trip.Cancel(command.Reason);
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "trip.cancelled",
            "trip",
            trip.Id.ToString(),
            before: JsonSerializer.Serialize(new
            {
                status = previousStatus.ToStorageValue(),
                cancelReason = (string?)null,
            }),
            after: JsonSerializer.Serialize(new
            {
                status = trip.Status.ToStorageValue(),
                cancelReason = trip.CancelReason,
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
