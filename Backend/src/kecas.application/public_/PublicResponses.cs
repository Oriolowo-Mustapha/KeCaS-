namespace kecas.application.public_;

/// <summary>
/// Anonymous response shapes for the public read endpoints (FR-09/11/13/22,
/// DD-10). Deliberately minimal: no operator internals, no staff data
/// (NFR-05 — availability is counts, verify-vehicle is first names only).
/// </summary>
public sealed record PublicRouteStopItem(string StopCode, string StopName, int Seq, int MinsFromStart);

public sealed record PublicRouteItem(
    string Code,
    string Name,
    string Category,
    string? Description,
    IReadOnlyList<PublicRouteStopItem> Stops);

public sealed record PublicRoutesResponse(IReadOnlyList<PublicRouteItem> Items);

/// <summary>§6.3: the fare response carries its category, zone/route codes, the amount in kobo and effective dates.</summary>
public sealed record PublicFareItem(
    string Scope,
    string? RouteCode,
    string? Category,
    string? FromZoneCode,
    string? ToZoneCode,
    int AmountKobo,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    DateTime? UpdatedAtUtc);

public sealed record PublicFaresResponse(IReadOnlyList<PublicFareItem> Items);

/// <summary>§5.1 availability values, as shown publicly (counts only, NFR-05).</summary>
public sealed record PublicAvailabilityCountItem(string Value, int Count);

/// <summary>
/// DD-06: availability is DERIVED — counts by availability value only, no
/// individual driver data (NFR-05). AsOfUtc lets the UI grey out stale data.
/// </summary>
public sealed record PublicAvailabilityResponse(
    DateTime AsOfUtc,
    IReadOnlyList<PublicAvailabilityCountItem> Items,
    int Total);

/// <summary>FR-22: lets a passenger verify a vehicle before boarding.</summary>
public sealed record VerifyVehicleResponse(
    string VehicleCode,
    string PlateNumber,
    string Category,
    string OperatorName,
    string? DriverFirstName);
