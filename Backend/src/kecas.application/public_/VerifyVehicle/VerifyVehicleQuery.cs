namespace kecas.application.public_.VerifyVehicle;

/// <summary>
/// GET /public/verify-vehicle?plate=... (FR-22): a passenger checks a vehicle
/// before boarding. Deliberately returns only safe fields — no driver contact
/// details, no licence data.
/// </summary>
public sealed record VerifyVehicleQuery(string PlateNumber) : IQuery<VerifyVehicleResponse>;
