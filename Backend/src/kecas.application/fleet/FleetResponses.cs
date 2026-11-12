namespace kecas.application.fleet;

using kecas.domain.common;

/// <summary>Shared response shapes for the fleet module (FR-04/05/06).</summary>
public sealed record VehicleListItem(
    Guid Id,
    string Code,
    string PlateNumber,
    VehicleCategory Category,
    int Capacity,
    string Status,
    DateTime StatusChangedAt,
    Guid OperatorId);

public sealed record ListVehiclesResponse(IReadOnlyList<VehicleListItem> Items, int Page, int PageSize, int Total);

/// <summary>FR-06: licence numbers are masked in every response.</summary>
public sealed record DriverListItem(
    Guid Id,
    Guid? UserId,
    Guid OperatorId,
    string LicenceMasked,
    DateOnly LicenceExpiry,
    string Status);

public sealed record ListDriversResponse(IReadOnlyList<DriverListItem> Items, int Page, int PageSize, int Total);

/// <summary>
/// FR-05/NFR-05: licence numbers are masked everywhere they leave the
/// Application layer (API responses and audit snapshots).
/// </summary>
internal static class LicenceMasker
{
    public static string Mask(string licence)
    {
        if (licence.Length <= 4)
        {
            return new string('*', licence.Length);
        }

        return $"{licence[..3]}{new string('*', licence.Length - 5)}{licence[^2..]}";
    }
}
