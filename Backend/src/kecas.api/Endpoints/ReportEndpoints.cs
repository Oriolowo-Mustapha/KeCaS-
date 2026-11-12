namespace kecas.api.Endpoints;

using kecas.application.abstractions.messaging;
using kecas.application.reports;
using kecas.domain.abstractions;
using kecas.domain.common;
using kecas.domain.fares;
using kecas.domain.network;
using MediatR;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Report endpoints (FR-19): the five default reports — trips by period,
/// fleet status, driver activity, availability history and fare history.
/// JSON by default; format=csv exports the same rows as a CSV file, so
/// CSV totals always match the database counts (TC-RPT-01). Access is
/// reports:view (§8.1): operator, manager and admin.
/// </summary>
public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reports")
            .WithTags("Reports");

        group.MapGet("/trips", async (
            ISender sender,
            CancellationToken cancellationToken,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] Guid? routeId = null,
            [FromQuery] Guid? vehicleId = null,
            [FromQuery] string? format = null) =>
        {
            var result = await sender.Send(
                new TripsReportQuery(from, to, routeId, vehicleId), cancellationToken);
            return Render(result, format);
        })
        .RequirePermission("reports:view")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/fleet", async (
            ISender sender,
            CancellationToken cancellationToken,
            [FromQuery] string? category = null,
            [FromQuery] Guid? operatorId = null,
            [FromQuery] string? format = null) =>
        {
            var result = await sender.Send(
                new FleetReportQuery(
                    EnumQueryParser.Parse<VehicleCategory>(category),
                    operatorId),
                cancellationToken);
            return Render(result, format);
        })
        .RequirePermission("reports:view")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/drivers", async (
            ISender sender,
            CancellationToken cancellationToken,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? format = null) =>
        {
            var result = await sender.Send(
                new DriversReportQuery(from, to), cancellationToken);
            return Render(result, format);
        })
        .RequirePermission("reports:view")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/availability-history", async (
            ISender sender,
            CancellationToken cancellationToken,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? format = null) =>
        {
            var result = await sender.Send(
                new AvailabilityHistoryReportQuery(from, to), cancellationToken);
            return Render(result, format);
        })
        .RequirePermission("reports:view")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/fare-history", async (
            ISender sender,
            CancellationToken cancellationToken,
            [FromQuery] string? category = null,
            [FromQuery] string? format = null) =>
        {
            var result = await sender.Send(
                new FareHistoryReportQuery(EnumQueryParser.Parse<RouteCategory>(category)),
                cancellationToken);
            return Render(result, format);
        })
        .RequirePermission("reports:view")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    /// <summary>JSON by default; a uniform CSV export when format=csv.</summary>
    private static IResult Render(Result<ReportTable> result, string? format)
    {
        if (!result.IsSuccess)
        {
            return VehicleEndpoints.ErrorToProblem(result.Error);
        }

        return string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase)
            ? Results.Bytes(
                System.Text.Encoding.UTF8.GetBytes(Csv.FromTable(result.Value)),
                "text/csv; charset=utf-8",
                fileDownloadName: $"kecas-{result.Value.Report}.csv")
            : Results.Ok(result.Value);
    }
}
