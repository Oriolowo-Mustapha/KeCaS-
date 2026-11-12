namespace kecas.api.Endpoints;

using kecas.application;
using kecas.application.abstractions.messaging;
using kecas.application.trips.CancelTrip;
using kecas.application.trips.CompleteTrip;
using kecas.application.trips.CreateTrip;
using kecas.application.trips.ListMyTrips;
using kecas.application.trips.ListTrips;
using kecas.application.trips.StartTrip;
using kecas.application.trips.UpdateTrip;
using kecas.domain.abstractions;
using kecas.domain.trips;
using MediatR;

public static class TripEndpoints
{
    public static IEndpointRouteBuilder MapTripEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/trips")
            .WithTags("Trips");

        // FR-10/14/15 / §7.3: schedule a trip with assignment, or log a
        // walk-up run after the fact (source=LOGGED, created COMPLETED).
        // All §7.3 guards (route, vehicle, driver, overlap) run in the handler.
        group.MapPost("", async (
            CreateTripRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new CreateTripCommand(
                    actor.Id,
                    request.RouteId,
                    request.VehicleId,
                    request.DriverId,
                    request.StartsAtUtc,
                    request.EndsAtUtc,
                    request.Source),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/trips/{result.Value}", new { id = result.Value })
                : TripEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("trips:manage")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // FR-14 conventions: page/page_size plus filters (status, route,
        // vehicle, driver, from/to). Manager/staff view (trips:manage).
        group.MapGet("", async (
            ISender sender,
            int? page,
            int? pageSize,
            string? status,
            Guid? routeId,
            Guid? vehicleId,
            Guid? driverId,
            DateTime? from,
            DateTime? to,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListTripsQuery(
                    page ?? 1,
                    pageSize ?? 20,
                    EnumQueryParser.Parse<TripStatus>(status),
                    routeId,
                    vehicleId,
                    driverId,
                    from,
                    to),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : TripEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("trips:manage")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // §8.1: drivers see their own trips without trips:manage.
        group.MapGet("/mine", async (
            ISender sender,
            HttpContext http,
            int? page,
            int? pageSize,
            string? status,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new ListMyTripsQuery(
                    actor.Id,
                    page ?? 1,
                    pageSize ?? 20,
                    EnumQueryParser.Parse<TripStatus>(status)),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : TripEndpoints.ErrorToProblem(result.Error);
        })
        .RequireSession()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // FR-14: reschedule or reassign a SCHEDULED trip; guards re-run in the
        // handler and a stale row_version is a 409 (§6.1).
        group.MapPatch("/{id:guid}", async (
            Guid id,
            UpdateTripRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new UpdateTripCommand(
                    actor.Id,
                    id,
                    request.StartsAtUtc,
                    request.EndsAtUtc,
                    request.RouteId,
                    request.VehicleId,
                    request.DriverId,
                    request.RowVersion),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : TripEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("trips:manage")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // §5.3: SCHEDULED → IN_PROGRESS. Manager or the assigned driver; the
        // start guard (vehicle on an open shift with the same driver) is in
        // the handler. Literal routes register before /{id:guid} subpaths so
        // "mine"/"start" are never swallowed.
        group.MapPost("/{id:guid}/start", async (
            Guid id,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new StartTripCommand(actor.Id, actor.Permissions, id),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : TripEndpoints.ErrorToProblem(result.Error);
        })
        .RequireSession()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // §5.3: IN_PROGRESS → COMPLETED with the actual end time.
        group.MapPost("/{id:guid}/complete", async (
            Guid id,
            CompleteTripRequest? request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new CompleteTripCommand(actor.Id, actor.Permissions, id, request?.ActualEndUtc),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : TripEndpoints.ErrorToProblem(result.Error);
        })
        .RequireSession()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // FR-15: a live trip → CANCELLED with a mandatory reason.
        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            CancelTripRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new CancelTripCommand(actor.Id, actor.Permissions, id, request.Reason),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : TripEndpoints.ErrorToProblem(result.Error);
        })
        .RequireSession()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    /// <summary>Same §6.1 envelope mapping as the other endpoint groups.</summary>
    private static IResult ErrorToProblem(Error error) => VehicleEndpoints.ErrorToProblem(error);
}

public sealed record CreateTripRequest(
    Guid RouteId,
    Guid VehicleId,
    Guid DriverId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    TripSource Source = TripSource.Scheduled);

public sealed record UpdateTripRequest(
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    Guid? RouteId,
    Guid? VehicleId,
    Guid? DriverId,
    long? RowVersion);

public sealed record CompleteTripRequest(DateTime? ActualEndUtc);

public sealed record CancelTripRequest(string Reason);
