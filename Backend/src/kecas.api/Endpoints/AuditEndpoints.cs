namespace kecas.api.Endpoints;

using kecas.application.abstractions.messaging;
using kecas.application.audit.ListAuditEvents;
using MediatR;

/// <summary>
/// FR-20: admin reads the append-only audit log (DD-11). Filters: actor,
/// entity type, date range (§6.2).
/// </summary>
public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/audit-events", async (
            ISender sender,
            int? page,
            int? pageSize,
            Guid? actorId,
            string? entityType,
            DateTime? from,
            DateTime? to,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListAuditEventsQuery(page ?? 1, pageSize ?? 20, actorId, entityType, from, to),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("users:manage")
        .WithTags("Audit")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        return app;
    }
}
