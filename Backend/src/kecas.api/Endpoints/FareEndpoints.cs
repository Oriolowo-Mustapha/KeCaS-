using kecas.application.abstractions.messaging;
using kecas.application.fares.ApproveFare;
using kecas.application.fares.CreateFare;
using kecas.application.fares.ListFares;
using kecas.application.fares.RejectFare;
using kecas.application.fares.SubmitFare;
using kecas.domain.fares;
using kecas.domain.network;
using MediatR;

namespace kecas.api.Endpoints;

/// <summary>
/// Fare workflow endpoints (FR-08/09): propose (fares:propose), approve or
/// reject (fares:approve, §5.3 — approver must differ from the proposer).
/// </summary>
public static class FareEndpoints
{
    public static IEndpointRouteBuilder MapFareEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/fares").WithTags("Fares");

        group.MapGet("", async (
            ISender sender,
            int? page,
            int? pageSize,
            string? category,
            string? status,
            Guid? routeId,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListFaresQuery(page ?? 1, pageSize ?? 20, EnumQueryParser.Parse<FareStatus>(status), routeId),
                cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("fares:propose")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("", async (
            CreateFareRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new CreateFareCommand(
                    actor.Id,
                    request.RouteId,
                    request.Category,
                    request.FromZoneId,
                    request.ToZoneId,
                    request.AmountKobo,
                    request.EffectiveFrom),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/fares/{result.Value}", new { id = result.Value })
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("fares:propose")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // §5.2: DRAFT → PENDING_APPROVAL. The proposer may submit their own fare.
        group.MapPost("/{id:guid}/submit", async (
            Guid id,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(new SubmitFareCommand(actor.Id, id), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("fares:propose")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // §5.3: approval publishes the fare and supersedes the currently
        // published fare for the same scope — all in one transaction.
        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(new ApproveFareCommand(actor.Id, id), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("fares:approve")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/reject", async (
            Guid id,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(new RejectFareCommand(actor.Id, id), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("fares:approve")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        return app;
    }
}

public sealed record CreateFareRequest(
    Guid? RouteId,
    RouteCategory? Category,
    Guid? FromZoneId,
    Guid? ToZoneId,
    int AmountKobo,
    DateOnly EffectiveFrom);
