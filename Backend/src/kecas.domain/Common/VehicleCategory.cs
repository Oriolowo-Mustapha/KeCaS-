namespace kecas.domain.common;

/// <summary>
/// The three transport categories managed by the system (design doc §4, vehicles).
/// </summary>
public enum VehicleCategory
{
    Keke = 1,
    Cab = 2,
    Shuttle = 3,
}

/// <summary>
/// Storage values match the design doc: KEKE, CAB, SHUTTLE.
/// </summary>
public static class VehicleCategoryExtensions
{
    public static string ToStorageValue(this VehicleCategory category) => category switch
    {
        VehicleCategory.Keke => "KEKE",
        VehicleCategory.Cab => "CAB",
        VehicleCategory.Shuttle => "SHUTTLE",
        _ => throw new ArgumentOutOfRangeException(nameof(category)),
    };
}
