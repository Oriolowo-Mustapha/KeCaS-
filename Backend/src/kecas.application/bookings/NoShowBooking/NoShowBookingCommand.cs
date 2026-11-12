namespace kecas.application.bookings.NoShowBooking;

/// <summary>POST /bookings/:id/no-show (FR-17): the confirmed passenger never showed.</summary>
public sealed record NoShowBookingCommand(Guid ActorId, Guid BookingId, string? Reason) : ICommand;
