namespace kecas.application.fleet.CreateVehicle;

using kecas.domain.common;

/// <summary>
/// Register a new vehicle (FR-04 / UC-02). Guards: unique vehicle code and
/// plate (BR-01 / NFR-07), vehicle must belong to a real operator, capacity
/// within 1..60.
/// </summary>
public sealed record CreateVehicleCommand(
    string Code,
    string PlateNumber,
    VehicleCategory Category,
    int Capacity,
    Guid OperatorId) : ICommand<Guid>;
