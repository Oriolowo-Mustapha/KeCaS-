namespace kecas.application.fleet.ChangeVehicleStatus;

using kecas.domain.common;

/// <summary>
/// POST /vehicles/:id/status (FR-05): change status with a mandatory reason;
/// every transition is audit-logged and feeds the vehicle status history.
/// </summary>
public sealed record ChangeVehicleStatusCommand(
    Guid ActorId,
    Guid VehicleId,
    VehicleStatus Status,
    string Reason) : ICommand;
