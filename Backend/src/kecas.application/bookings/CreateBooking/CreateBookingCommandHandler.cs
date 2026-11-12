namespace kecas.application.bookings.CreateBooking;

using System.Text.Json;
using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;
using kecas.domain.bookings;
using kecas.domain.network;
using kecas.domain.trips;

internal sealed class CreateBookingCommandHandler(
    ITripRepository tripRepository,
    IBookingRepository bookingRepository,
    IStopRepository stopRepository,
    IDateTimeProvider dateTimeProvider,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateBookingCommand, CreateBookingResult>
{
    public async Task<Result<CreateBookingResult>> Handle(
        CreateBookingCommand command,
        CancellationToken cancellationToken)
    {
        var trip = await tripRepository.GetByIdAsync(command.TripId, cancellationToken);
        if (trip is null)
        {
            return Result.Failure<CreateBookingResult>(TripErrors.NotFound);
        }

        // §7.4 step 1: bookings target future scheduled trips.
        if (trip.Status != TripStatus.Scheduled || trip.StartsAt <= dateTimeProvider.UtcNow)
        {
            return Result.Failure<CreateBookingResult>(BookingErrors.TripNotBookable);
        }

        if (command.PickupStopId is { } pickup
            && await stopRepository.GetByIdAsync(pickup, cancellationToken) is null)
        {
            return Result.Failure<CreateBookingResult>(BookingErrors.StopNotFound);
        }

        if (command.DropoffStopId is { } dropoff
            && await stopRepository.GetByIdAsync(dropoff, cancellationToken) is null)
        {
            return Result.Failure<CreateBookingResult>(BookingErrors.StopNotFound);
        }

        var bookingCode = $"BKG-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var created = Booking.Request(
            command.TripId,
            command.ActorId,
            command.PickupStopId,
            command.DropoffStopId,
            command.PartySize,
            dateTimeProvider.UtcNow,
            bookingCode);
        if (created.IsFailure)
        {
            return Result.Failure<CreateBookingResult>(created.Error);
        }

        bookingRepository.Add(created.Value);

        auditWriter.Append(
            command.ActorId,
            "booking.requested",
            "booking",
            created.Value.Id.ToString(),
            after: JsonSerializer.Serialize(new
            {
                bookingCode = created.Value.BookingCode,
                tripId = created.Value.TripId,
                partySize = created.Value.PartySize,
                status = created.Value.Status.ToStorageValue(),
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateBookingResult(created.Value.Id, created.Value.BookingCode));
    }
}
