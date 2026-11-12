namespace kecas.domain.fleet;

/// <summary>
/// Who runs the vehicles — lets one system hold university-run and
/// independent operators (design doc §4.2, operators).
/// </summary>
public enum OperatorKind
{
    University = 1,
    Cooperative = 2,
    Independent = 3,
}

/// <summary>
/// Stored vehicle status (design doc §5.1). Public availability is DERIVED
/// from this plus open shifts and trip state — never typed by staff (DD-06).
/// </summary>
public enum VehicleStatus
{
    Active = 1,
    Inactive = 2,
    Maintenance = 3,
    Retired = 4,
}

/// <summary>
/// Stored driver status.
/// </summary>
public enum DriverStatus
{
    Active = 1,
    Suspended = 2,
    Retired = 3,
}
