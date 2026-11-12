namespace kecas.application.bookings.ConfirmBooking;

using System.Text.Json;
using kecas.application.abstractions.data;
using kecas.domain.bookings;
using kecas.domain.fleet;
using kecas.domain.trips;

internal sealed class ConfirmBookingCommandHandler(
    IBookingRepository bookingRepository,
    ITripRepository tripRepository,
    IVehicleRepository vehicleRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ConfirmBookingCommand, ConfirmBookingResult>
{
    public async Task<Result<ConfirmBookingResult>> Handle(
        ConfirmBookingCommand command,
        CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(command.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<ConfirmBookingResult>(BookingErrors.NotFound);
        }

        // Status guard before the lock: only REQUESTED bookings are confirmable.
        if (booking.Status != BookingStatus.Requested)
        {
            return Result.Failure<ConfirmBookingResult>(BookingErrors.NotConfirmable);
        }

        // §7.4 step 4: everything between BEGIN and COMMIT is serialized per
        // trip by the row lock, so the capacity sum cannot race a confirm.
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var trip = await tripRepository.GetByIdAsync(booking.TripId, cancellationToken);
            if (trip is null)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<ConfirmBookingResult>(TripErrors.NotFound);
            }

            var locked = await tripRepository.TryLockAsync(trip.Id, cancellationToken);
            if (!locked)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<ConfirmBookingResult>(TripErrors.NotFound);
            }

            var capacity = 0;
            if (trip.VehicleId is { } vehicleId)
            {
                var vehicle = await vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
                capacity = vehicle?.Capacity ?? 0;
            }

            var confirmedSeats =
                await bookingRepository.SumConfirmedPartySizeAsync(trip.Id, cancellationToken);

            if (confirmedSeats + booking.PartySize > capacity)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<ConfirmBookingResult>(BookingErrors.CapacityExceeded);
            }

            var rideCode = $"RIDE-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

            var confirmed = booking.Confirm(command.ActorId, rideCode);
            if (confirmed.IsFailure)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<ConfirmBookingResult>(confirmed.Error);
            }

            auditWriter.Append(
                command.ActorId,
                "booking.confirmed",
                "booking",
                booking.Id.ToString(),
                before: JsonSerializer.Serialize(new { status = "REQUESTED", rideCode = (string?)null }),
                after: JsonSerializer.Serialize(new
                {
                    status = booking.Status.ToStorageValue(),
                    rideCode = booking.RideCode,
                }));

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result.Success(new ConfirmBookingResult(booking.RideCode!));
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
