namespace kecas.application.bookings.CompleteBooking;

/// <summary>POST /bookings/:id/complete (FR-17): the trip ran with the passenger aboard.</summary>
public sealed record CompleteBookingCommand(Guid ActorId, Guid BookingId) : ICommand;
