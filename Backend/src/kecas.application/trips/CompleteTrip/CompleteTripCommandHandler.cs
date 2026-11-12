namespace kecas.application.trips.CompleteTrip;

using System.Text.Json;
using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;
using kecas.domain.trips;

internal sealed class CompleteTripCommandHandler(
    ITripRepository tripRepository,
    IDriverRepository driverRepository,
    IAuditWriter auditWriter,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CompleteTripCommand>
{
    private const string ManageTripsPermission = "trips:manage";

    public async Task<Result> Handle(CompleteTripCommand command, CancellationToken cancellationToken)
    {
        var trip = await tripRepository.GetByIdAsync(command.TripId, cancellationToken);
        if (trip is null)
        {
            return Result.Failure(TripErrors.NotFound);
        }

        // trips:manage, or the assigned driver acting on their own trip.
        if (!command.ActorPermissions.Contains(ManageTripsPermission))
        {
            var ownDriver = await driverRepository.GetByUserIdAsync(command.ActorId, cancellationToken);
            if (ownDriver is null || ownDriver.Id != trip.DriverId)
            {
                return Result.Failure(AuthErrors.PermissionDenied);
            }
        }

        var actualEnd = command.ActualEndUtc ?? dateTimeProvider.UtcNow;

        var result = trip.Complete(actualEnd);
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "trip.completed",
            "trip",
            trip.Id.ToString(),
            before: JsonSerializer.Serialize(new
            {
                status = TripStatus.InProgress.ToStorageValue(),
                endsAt = trip.EndsAt,
            }),
            after: JsonSerializer.Serialize(new
            {
                status = trip.Status.ToStorageValue(),
                endsAt = trip.EndsAt,
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
