namespace kecas.application.shifts;

/// <summary>Shared response shapes for the shifts module (FR-11).</summary>
public sealed record ShiftListItem(
    Guid Id,
    Guid DriverId,
    Guid VehicleId,
    Guid? RankStopId,
    string? RankStopCode,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string? ClosedBy);

public sealed record ListShiftsResponse(IReadOnlyList<ShiftListItem> Items, int Page, int PageSize, int Total);
