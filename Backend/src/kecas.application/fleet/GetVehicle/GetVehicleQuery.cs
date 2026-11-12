namespace kecas.application.fleet.GetVehicle;

using kecas.application.fleet;

/// <summary>Vehicle detail (GET /vehicles/:id).</summary>
public sealed record GetVehicleQuery(Guid VehicleId) : IQuery<VehicleListItem>;
