namespace kecas.api.Endpoints;

using kecas.application.abstractions.messaging;
using kecas.application.fleet;
using kecas.application.fleet.CreateDriver;
using kecas.application.fleet.ListDrivers;
using kecas.application.fleet.UpdateDriver;
using kecas.domain.fleet;
using MediatR;

/// <summary>
/// FR-06: driver records. Reads need vehicles:view; writes need
/// vehicles:manage (§8.1). Licence numbers are always masked in responses.
/// </summary>
public static class DriverEndpoints
{
    public static IEndpointRouteBuilder MapDriverEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/drivers")
            .WithTags("Drivers");

        group.MapGet("", async (
            ISender sender,
            int? page,
            int? pageSize,
            Guid? operatorId,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListDriversQuery(page ?? 1, pageSize ?? 20, operatorId), cancellationToken);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("vehicles:view")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("", async (
            CreateDriverRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var command = new CreateDriverCommand(
                actor.Id,
                request.UserId,
                request.OperatorId,
                request.LicenceNumber,
                request.LicenceExpiry);

            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/v1/drivers/{result.Value}", new { id = result.Value })
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("vehicles:manage")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        group.MapPatch("/{id:guid}", async (
            Guid id,
            UpdateDriverRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var command = new UpdateDriverCommand(
                actor.Id,
                id,
                request.LicenceNumber,
                request.LicenceExpiry,
                request.Status);

            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess
                ? Results.NoContent()
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("vehicles:manage")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        return app;
    }
}

public sealed record CreateDriverRequest(
    Guid? UserId,
    Guid OperatorId,
    string LicenceNumber,
    DateOnly LicenceExpiry);

public sealed record UpdateDriverRequest(
    string? LicenceNumber,
    DateOnly? LicenceExpiry,
    DriverStatus? Status);
