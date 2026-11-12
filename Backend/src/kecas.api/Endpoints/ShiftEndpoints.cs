namespace kecas.api.Endpoints;

using kecas.application;
using kecas.application.abstractions.messaging;
using kecas.application.shifts.CheckIn;
using kecas.application.shifts.CheckOut;
using kecas.application.shifts.ListShifts;
using kecas.domain.abstractions;
using MediatR;

public static class ShiftEndpoints
{
    public static IEndpointRouteBuilder MapShiftEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/shifts")
            .WithTags("Shifts");

        // FR-11 / §7.2: drivers check themselves in; operators and managers
        // (drivers:checkin) may check a driver in on their behalf by passing
        // driver_id. The command enforces that split, so the endpoint only
        // requires a live session.
        group.MapPost("/check-in", async (
            CheckInRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new CheckInCommand(
                    actor.Id,
                    actor.Permissions,
                    request.DriverId,
                    request.VehicleId,
                    request.RankStopId),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/shifts/{result.Value}", new { id = result.Value })
                : ShiftEndpoints.ErrorToProblem(result.Error);
        })
        .RequireSession()
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // FR-11: drivers may close their own shift; drivers:checkin closes any.
        group.MapPost("/{id:guid}/check-out", async (
            Guid id,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new CheckOutCommand(actor.Id, actor.Permissions, id),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : ShiftEndpoints.ErrorToProblem(result.Error);
        })
        .RequireSession()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // Shift records are staff data: those who manage check-ins may view.
        group.MapGet("", async (
            ISender sender,
            int? page,
            int? pageSize,
            bool? openOnly,
            Guid? driverId,
            Guid? vehicleId,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListShiftsQuery(page ?? 1, pageSize ?? 20, openOnly, driverId, vehicleId),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ShiftEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("drivers:checkin")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        return app;
    }

    /// <summary>Same §6.1 envelope mapping as the other endpoint groups.</summary>
    private static IResult ErrorToProblem(Error error) => VehicleEndpoints.ErrorToProblem(error);
}

public sealed record CheckInRequest(Guid? DriverId, Guid VehicleId, Guid? RankStopId);
