namespace kecas.application.abstractions.data;

using kecas.domain.fares;
using kecas.domain.fleet;
using kecas.domain.network;

/// <summary>
/// Read-only report aggregations (FR-19). Implemented in Infrastructure with
/// EF Core; every method returns plain tabular rows that the API renders as
/// JSON or CSV from the same data, so CSV totals always match the JSON.
/// </summary>
public interface IReportQueries
{
    /// <summary>Trips per day grouped by status ("trips by period" report).</summary>
    Task<IReadOnlyList<TripsDayRow>> TripsByDayAsync(
        DateTime? from, DateTime? to, Guid? routeId, Guid? vehicleId,
        CancellationToken cancellationToken = default);

    /// <summary>Vehicles grouped by category and stored status ("fleet status" report).</summary>
    Task<IReadOnlyList<FleetStatusRow>> FleetByStatusAsync(
        VehicleCategory? category, Guid? operatorId,
        CancellationToken cancellationToken = default);

    /// <summary>Trips per driver in the period ("driver activity" report).</summary>
    Task<IReadOnlyList<DriverActivityRow>> DriverActivityAsync(
        DateTime? from, DateTime? to,
        CancellationToken cancellationToken = default);

    /// <summary>Shift check-ins and check-outs per day ("availability history" report).</summary>
    Task<IReadOnlyList<AvailabilityDayRow>> AvailabilityHistoryAsync(
        DateTime? from, DateTime? to,
        CancellationToken cancellationToken = default);

    /// <summary>Every fare record with its lifecycle dates ("fare history" report).</summary>
    Task<IReadOnlyList<FareHistoryRow>> FareHistoryAsync(
        RouteCategory? category,
        CancellationToken cancellationToken = default);
}

public sealed record TripsDayRow(
    DateOnly Date,
    int Scheduled,
    int InProgress,
    int Completed,
    int Cancelled,
    int Total);

public sealed record FleetStatusRow(
    string Category,
    string Status,
    int Vehicles);

public sealed record DriverActivityRow(
    Guid DriverId,
    string? DriverName,
    int Scheduled,
    int Completed,
    int Cancelled,
    int Total);

public sealed record AvailabilityDayRow(
    DateOnly Date,
    int CheckIns,
    int CheckOuts,
    int OpenAtEndOfDay);

public sealed record FareHistoryRow(
    Guid FareId,
    string? RouteCode,
    string? Category,
    long AmountKobo,
    string Status,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    DateTime? ApprovedAtUtc);
