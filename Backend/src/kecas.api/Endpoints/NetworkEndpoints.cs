using kecas.application.abstractions.messaging;
using kecas.application.network.CreateRoute;
using kecas.application.network.CreateStop;
using kecas.application.network.CreateZone;
using kecas.application.network.ListRoutes;
using kecas.application.network.ListStops;
using kecas.application.network.ListZones;
using kecas.application.network.SetRouteStops;
using kecas.application.network.UpdateRoute;
using kecas.domain.network;
using MediatR;

namespace kecas.api.Endpoints;

/// <summary>
/// Network admin endpoints (FR-07): zones, stops and routes. Every access
/// needs network:manage per §8.1 (the public site reads a cached projection).
/// </summary>
public static class NetworkEndpoints
{
    public static IEndpointRouteBuilder MapNetworkEndpoints(this IEndpointRouteBuilder app)
    {
        var zones = app.MapGroup("/api/v1/zones").WithTags("Network");

        zones.MapGet("", async (
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListZonesQuery(), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("network:manage")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        zones.MapPost("", async (
            CreateZoneRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new CreateZoneCommand(actor.Id, request.Code, request.Name),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/zones/{result.Value}", new { id = result.Value })
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("network:manage")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status409Conflict);

        var stops = app.MapGroup("/api/v1/stops").WithTags("Network");

        stops.MapGet("", async (
            ISender sender,
            Guid? zoneId,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListStopsQuery(zoneId), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("network:manage")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        stops.MapPost("", async (
            CreateStopRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new CreateStopCommand(actor.Id, request.Code, request.Name, request.ZoneId, request.IsRank),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/stops/{result.Value}", new { id = result.Value })
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("network:manage")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        var routes = app.MapGroup("/api/v1/routes").WithTags("Network");

        routes.MapGet("", async (
            ISender sender,
            string? category,
            bool? isActive,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListRoutesQuery(EnumQueryParser.Parse<RouteCategory>(category), isActive, page ?? 1, pageSize ?? 20),
                cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("network:manage")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        routes.MapPost("", async (
            CreateRouteRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new CreateRouteCommand(
                    actor.Id,
                    request.Code,
                    request.Name,
                    request.Category,
                    request.Description,
                    request.Stops.Select(s => new RouteStopInput(s.StopId, s.Seq, s.MinsFromStart)).ToList()),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/routes/{result.Value}", new { id = result.Value })
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("network:manage")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        routes.MapPatch("/{id:guid}", async (
            Guid id,
            UpdateRouteRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new UpdateRouteCommand(actor.Id, id, request.Name, request.Description, request.IsActive),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("network:manage")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        routes.MapPut("/{id:guid}/stops", async (
            Guid id,
            SetRouteStopsRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new SetRouteStopsCommand(
                    actor.Id,
                    id,
                    request.Stops.Select(s => new RouteStopInput(s.StopId, s.Seq, s.MinsFromStart)).ToList()),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("network:manage")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}

public sealed record CreateZoneRequest(string Code, string Name);

public sealed record CreateStopRequest(string Code, string Name, Guid ZoneId, bool IsRank = false);

public sealed record RouteStopRequestBody(Guid StopId, int Seq, int MinsFromStart = 0);

public sealed record CreateRouteRequest(
    string Code,
    string Name,
    RouteCategory Category,
    string? Description,
    IReadOnlyList<RouteStopRequestBody> Stops);

public sealed record UpdateRouteRequest(string? Name, string? Description, bool? IsActive);

public sealed record SetRouteStopsRequest(IReadOnlyList<RouteStopRequestBody> Stops);
