namespace kecas.application.fleet.UpdateVehicle;

/// <summary>
/// PATCH /vehicles/:id (FR-04). Identity fields (code, plate, category) are
/// immutable; capacity is the editable attribute.
/// </summary>
public sealed record UpdateVehicleCommand(Guid ActorId, Guid VehicleId, int? Capacity) : ICommand;
