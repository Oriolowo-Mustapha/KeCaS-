namespace kecas.infrastructure.Configurations;

using kecas.domain.common;
using kecas.domain.fares;
using kecas.domain.fleet;
using kecas.domain.network;
using kecas.domain.trips;
using kecas.domain.users;

/// <summary>
/// String storage values for enums, matching the design doc and the database
/// CHECK constraints: KEKE / CAB / SHUTTLE, ACTIVE / INACTIVE / MAINTENANCE /
/// RETIRED, UNIVERSITY / COOPERATIVE / INDEPENDENT, ACTIVE / DISABLED, ...
/// Static methods (not switch expressions) because EF value-conversion lambdas
/// are expression trees.
/// </summary>
internal static class EnumStorage
{
    public static string VehicleCategoryTo(VehicleCategory category) => category.ToStorageValue();

    public static VehicleCategory VehicleCategoryFrom(string value) => value switch
    {
        "KEKE" => VehicleCategory.Keke,
        "CAB" => VehicleCategory.Cab,
        "SHUTTLE" => VehicleCategory.Shuttle,
        _ => throw new InvalidOperationException($"Unknown vehicle category '{value}'."),
    };

    public static string VehicleStatusTo(VehicleStatus status) => status.ToString().ToUpperInvariant();

    public static VehicleStatus VehicleStatusFrom(string value) =>
        Enum.Parse<VehicleStatus>(value, ignoreCase: true);

    public static string OperatorKindTo(OperatorKind kind) => kind.ToString().ToUpperInvariant();

    public static OperatorKind OperatorKindFrom(string value) =>
        Enum.Parse<OperatorKind>(value, ignoreCase: true);

    public static string DriverStatusTo(DriverStatus status) => status.ToString().ToUpperInvariant();

    public static DriverStatus DriverStatusFrom(string value) =>
        Enum.Parse<DriverStatus>(value, ignoreCase: true);

    public static string UserStatusTo(UserStatus status) => status.ToString().ToUpperInvariant();

    public static UserStatus UserStatusFrom(string value) =>
        Enum.Parse<UserStatus>(value, ignoreCase: true);

    public static string RouteCategoryTo(RouteCategory category) => category.ToString().ToUpperInvariant();

    public static RouteCategory RouteCategoryFrom(string value) =>
        Enum.Parse<RouteCategory>(value, ignoreCase: true);

    public static string FareStatusTo(FareStatus status) => status.ToStorageValue();

    public static FareStatus FareStatusFrom(string value) => value switch
    {
        "PENDING_APPROVAL" => FareStatus.PendingApproval,
        _ => Enum.Parse<FareStatus>(value, ignoreCase: true),
    };

    public static string TripStatusTo(TripStatus status) => status.ToStorageValue();

    public static TripStatus TripStatusFrom(string value) => value switch
    {
        "IN_PROGRESS" => TripStatus.InProgress,
        _ => Enum.Parse<TripStatus>(value, ignoreCase: true),
    };

    public static string TripSourceTo(TripSource source) => source.ToStorageValue();

    public static TripSource TripSourceFrom(string value) =>
        Enum.Parse<TripSource>(value, ignoreCase: true);

    public static string ShiftClosedByTo(ShiftClosedBy closedBy) => closedBy.ToString().ToUpperInvariant();

    public static ShiftClosedBy ShiftClosedByFrom(string value) =>
        Enum.Parse<ShiftClosedBy>(value, ignoreCase: true);
}
