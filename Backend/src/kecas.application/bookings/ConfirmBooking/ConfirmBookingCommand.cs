namespace kecas.application.bookings.ConfirmBooking;

/// <summary>
/// POST /bookings/:id/confirm (FR-17, §7.4): manager confirms a REQUESTED
/// booking. Locks the trip row, sums confirmed party_size, 409 when BR-09
/// capacity would be exceeded; otherwise confirms and issues a ride code.
/// </summary>
public sealed record ConfirmBookingCommand(Guid ActorId, Guid BookingId) : ICommand<ConfirmBookingResult>;

public sealed record ConfirmBookingResult(string RideCode);
