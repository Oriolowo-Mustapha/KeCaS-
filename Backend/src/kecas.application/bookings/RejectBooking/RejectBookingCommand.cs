namespace kecas.application.bookings.RejectBooking;

/// <summary>POST /bookings/:id/reject (FR-17): REQUESTED → REJECTED + reason.</summary>
public sealed record RejectBookingCommand(Guid ActorId, Guid BookingId, string? Reason) : ICommand;
