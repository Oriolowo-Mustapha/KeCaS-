using kecas.application.abstractions.messaging;
using kecas.application.public_.GetPublicAvailability;
using kecas.application.notices.GetPublicNotices;
using kecas.application.public_.GetPublicFares;
using kecas.application.public_.GetPublicRoutes;
using kecas.application.public_.VerifyVehicle;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;

namespace kecas.api.Endpoints;

/// <summary>
/// Anonymous public read endpoints (FR-09/11/13/22, DD-10). No session
/// required; responses are cached 60 seconds (NFR-02: cheap phones on weak
/// connections). verify-vehicle is POST per §6.2, rate limited 20/hour/IP
/// (DD-12). All state-changing /api requests need the CSRF header (§8.2).
/// </summary>
public static class PublicEndpoints
{
    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/public")
            .WithTags("Public")
            .CacheOutput("public60");

        // FR-13 / UC-01: published routes with ordered stops.
        group.MapGet("/routes", async (
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetPublicRoutesQuery(), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .Produces(StatusCodes.Status200OK);

        // FR-09: published fares with category and effective dates.
        group.MapGet("/fares", async (
            ISender sender,
            string? category,
            string? from,
            string? to,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetPublicFaresQuery(category, from, to),
                cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .Produces(StatusCodes.Status200OK);

        // FR-11: derived availability, counts only (NFR-05).
        group.MapGet("/availability", async (
            ISender sender,
            string? category,
            string? stop,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetPublicAvailabilityQuery(category, stop),
                cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .Produces(StatusCodes.Status200OK);

        // FR-22: currently active notices for the public site (passenger view).
        // Part of the notices module — hidden (404) when the flag is off.
        group.MapGet("/notices", async (
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetPublicNoticesQuery(), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequireFeature("Notices")
        .Produces(StatusCodes.Status200OK);

        // FR-22 / DD-12: plate check returns minimal fields only.
        group.MapPost("/verify-vehicle", async (
            VerifyVehicleRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new VerifyVehicleQuery(request.PlateNumber), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        // DD-12: 20 requests per hour per IP so the registry cannot be scraped.
        .RequireRateLimiting("verify-vehicle")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        return app;
    }
}

public sealed record VerifyVehicleRequest(string PlateNumber);
