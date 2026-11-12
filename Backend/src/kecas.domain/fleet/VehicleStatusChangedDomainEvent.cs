namespace kecas.domain.fleet;

/// <summary>
/// Raised whenever a vehicle changes stored status. The vehicle_status_log is
/// persisted by Infrastructure, feeding the availability history report.
/// </summary>
public sealed record VehicleStatusChangedDomainEvent(
    Guid VehicleId,
    VehicleStatus PreviousStatus,
    VehicleStatus NewStatus,
    string Reason,
    Guid Id,
    DateTime OccurredAtUtc) : IDomainEvent;
