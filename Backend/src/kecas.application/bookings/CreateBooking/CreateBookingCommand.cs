namespace kecas.application.bookings.CreateBooking;

/// <summary>
/// POST /bookings (FR-16, DD-09): a signed-in passenger requests seats on a
/// future SCHEDULED trip. The booking starts as REQUESTED with a booking code.
/// </summary>
public sealed record CreateBookingCommand(
    Guid ActorId,
    Guid TripId,
    Guid? PickupStopId,
    Guid? DropoffStopId,
    int PartySize) : ICommand<CreateBookingResult>;

public sealed record CreateBookingResult(Guid BookingId, string BookingCode);
