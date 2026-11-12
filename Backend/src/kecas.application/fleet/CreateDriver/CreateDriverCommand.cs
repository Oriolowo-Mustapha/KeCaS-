namespace kecas.application.fleet.CreateDriver;

/// <summary>
/// Register a driver (FR-06 / UC-03). Licence expiry must be in the future;
/// the optional UserId links a signed-in account to the driver record.
/// </summary>
public sealed record CreateDriverCommand(
    Guid ActorId,
    Guid? UserId,
    Guid OperatorId,
    string LicenceNumber,
    DateOnly LicenceExpiry) : ICommand<Guid>;
