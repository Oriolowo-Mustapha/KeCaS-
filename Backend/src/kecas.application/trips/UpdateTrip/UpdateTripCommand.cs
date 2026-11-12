namespace kecas.application.trips.UpdateTrip;

/// <summary>
/// PATCH /trips/:id (FR-14): reschedule or reassign a SCHEDULED trip. The
/// §7.3 guards re-run for any changed reference; a stale row_version is a 409.
/// </summary>
public sealed record UpdateTripCommand(
    Guid ActorId,
    Guid TripId,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    Guid? RouteId,
    Guid? VehicleId,
    Guid? DriverId,
    long? RowVersion) : ICommand;
