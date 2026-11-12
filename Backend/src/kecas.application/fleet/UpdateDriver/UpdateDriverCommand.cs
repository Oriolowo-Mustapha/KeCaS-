namespace kecas.application.fleet.UpdateDriver;

/// <summary>
/// PATCH /drivers/:id (FR-06). Licence details may be corrected; the status
/// field moves the driver between ACTIVE / SUSPENDED / RETIRED.
/// </summary>
public sealed record UpdateDriverCommand(
    Guid ActorId,
    Guid DriverId,
    string? LicenceNumber,
    DateOnly? LicenceExpiry,
    DriverStatus? Status) : ICommand;
