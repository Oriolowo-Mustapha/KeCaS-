namespace kecas.application.reports;

using kecas.application.abstractions.messaging;
using kecas.domain.fares;
using kecas.domain.fleet;
using kecas.domain.network;

/// <summary>
/// FR-19: the uniform tabular shape every report returns. The API renders the
/// same rows as JSON or as CSV (format=csv), so both exports always agree.
/// </summary>
public sealed record ReportTable(
    string Report,
    DateTime GeneratedAtUtc,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows);

/// <summary>GET /reports/trips: trips per day by status ("trips by period").</summary>
public sealed record TripsReportQuery(
    DateTime? From,
    DateTime? To,
    Guid? RouteId,
    Guid? VehicleId) : IQuery<ReportTable>;

/// <summary>GET /reports/fleet: vehicles grouped by category and status.</summary>
public sealed record FleetReportQuery(
    VehicleCategory? Category,
    Guid? OperatorId) : IQuery<ReportTable>;

/// <summary>GET /reports/drivers: trips per driver in the period ("driver activity").</summary>
public sealed record DriversReportQuery(
    DateTime? From,
    DateTime? To) : IQuery<ReportTable>;

/// <summary>GET /reports/availability-history: shift check-ins/outs per day.</summary>
public sealed record AvailabilityHistoryReportQuery(
    DateTime? From,
    DateTime? To) : IQuery<ReportTable>;

/// <summary>GET /reports/fare-history: every fare record with lifecycle dates.</summary>
public sealed record FareHistoryReportQuery(
    RouteCategory? Category) : IQuery<ReportTable>;
