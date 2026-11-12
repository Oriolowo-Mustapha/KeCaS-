namespace kecas.application.bookings.CancelBooking;

/// <summary>
/// POST /bookings/:id/cancel (FR-16): the passenger withdraws their own
/// still-undecided request; bookings:manage staff may cancel any.
/// </summary>
public sealed record CancelBookingCommand(
    Guid ActorId,
    IReadOnlyList<string> ActorPermissions,
    Guid BookingId) : ICommand;
