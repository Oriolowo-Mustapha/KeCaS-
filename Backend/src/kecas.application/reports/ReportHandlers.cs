namespace kecas.application.reports;

using kecas.application.abstractions.data;
using kecas.application.abstractions.messaging;

/// <summary>Maps typed report rows onto the shared ReportTable shape.</summary>
internal static class ReportBuilder
{
    public static Result<ReportTable> Build<T>(
        string report,
        IReadOnlyList<T> rows,
        Func<T, object?[]> project,
        params string[] columns) =>
        Result.Success(new ReportTable(
            report,
            DateTime.UtcNow,
            columns,
            rows.Select(r => (IReadOnlyList<object?>)project(r)).ToList()));
}

internal sealed class TripsReportHandler(IReportQueries reports)
    : IQueryHandler<TripsReportQuery, ReportTable>
{
    public async Task<Result<ReportTable>> Handle(TripsReportQuery query, CancellationToken cancellationToken) =>
        ReportBuilder.Build(
            "trips",
            await reports.TripsByDayAsync(
                query.From, query.To, query.RouteId, query.VehicleId, cancellationToken),
            r => new object?[] { r.Date, r.Scheduled, r.InProgress, r.Completed, r.Cancelled, r.Total },
            "date", "scheduled", "in_progress", "completed", "cancelled", "total");
}

internal sealed class FleetReportHandler(IReportQueries reports)
    : IQueryHandler<FleetReportQuery, ReportTable>
{
    public async Task<Result<ReportTable>> Handle(FleetReportQuery query, CancellationToken cancellationToken) =>
        ReportBuilder.Build(
            "fleet",
            await reports.FleetByStatusAsync(query.Category, query.OperatorId, cancellationToken),
            r => new object?[] { r.Category, r.Status, r.Vehicles },
            "category", "status", "vehicles");
}

internal sealed class DriversReportHandler(IReportQueries reports)
    : IQueryHandler<DriversReportQuery, ReportTable>
{
    public async Task<Result<ReportTable>> Handle(DriversReportQuery query, CancellationToken cancellationToken) =>
        ReportBuilder.Build(
            "drivers",
            await reports.DriverActivityAsync(query.From, query.To, cancellationToken),
            r => new object?[] { r.DriverId, r.DriverName, r.Scheduled, r.Completed, r.Cancelled, r.Total },
            "driver_id", "driver_name", "scheduled", "completed", "cancelled", "total");
}

internal sealed class AvailabilityHistoryReportHandler(IReportQueries reports)
    : IQueryHandler<AvailabilityHistoryReportQuery, ReportTable>
{
    public async Task<Result<ReportTable>> Handle(
        AvailabilityHistoryReportQuery query, CancellationToken cancellationToken) =>
        ReportBuilder.Build(
            "availability-history",
            await reports.AvailabilityHistoryAsync(query.From, query.To, cancellationToken),
            r => new object?[] { r.Date, r.CheckIns, r.CheckOuts, r.OpenAtEndOfDay },
            "date", "check_ins", "check_outs", "open_at_end_of_day");
}

internal sealed class FareHistoryReportHandler(IReportQueries reports)
    : IQueryHandler<FareHistoryReportQuery, ReportTable>
{
    public async Task<Result<ReportTable>> Handle(FareHistoryReportQuery query, CancellationToken cancellationToken) =>
        ReportBuilder.Build(
            "fare-history",
            await reports.FareHistoryAsync(query.Category, cancellationToken),
            r => new object?[] { r.FareId, r.RouteCode, r.Category, r.AmountKobo, r.Status, r.EffectiveFrom, r.EffectiveTo, r.ApprovedAtUtc },
            "fare_id", "route_code", "category", "amount_kobo", "status", "effective_from", "effective_to", "approved_at");
}
