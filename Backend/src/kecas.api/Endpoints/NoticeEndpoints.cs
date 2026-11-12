namespace kecas.api.Endpoints;

using kecas.application.abstractions.messaging;
using kecas.application.notices.CreateNotice;
using kecas.application.notices.ExpireNotice;
using kecas.application.notices.GetPublicNotices;
using kecas.application.notices.ListNotices;
using kecas.application.notices.PublishNotice;
using kecas.application.notices.UpdateNotice;
using kecas.domain.abstractions;
using kecas.domain.notices;
using MediatR;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Notice board endpoints (FR-22), behind FeatureFlags:Notices. Managers with
/// trips:manage draft, publish and expire notices; the public site reads the
/// active ones from /public/notices (cached).
/// </summary>
public static class NoticeEndpoints
{
    public static IEndpointRouteBuilder MapNoticeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notices")
            .WithTags("Notices")
            .RequireFeature("Notices");

        // FR-22: create a DRAFT notice (publishing is a separate action).
        group.MapPost("", async (
            CreateNoticeRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new CreateNoticeCommand(
                    actor.Id,
                    request.Title,
                    request.Body,
                    request.Audience,
                    request.Category,
                    request.PublishAtUtc,
                    request.ExpiresAtUtc),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/notices/{result.Value}", new { id = result.Value })
                : NoticeEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("trips:manage")
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // FR-22: the staff notice board.
        group.MapGet("", async (
            ISender sender,
            CancellationToken cancellationToken,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? category = null) =>
        {
            var result = await sender.Send(
                new ListNoticesQuery(
                    page,
                    pageSize,
                    EnumQueryParser.Parse<NoticeStatus>(status),
                    EnumQueryParser.Parse<NoticeCategory>(category)),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : NoticeEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("trips:manage")
        .Produces(StatusCodes.Status200OK);

        // FR-22: edit a draft.
        group.MapPatch("/{id:guid}", async (
            Guid id,
            UpdateNoticeRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new UpdateNoticeCommand(
                    actor.Id,
                    id,
                    request.Title,
                    request.Body,
                    request.Audience,
                    request.Category,
                    request.PublishAtUtc,
                    request.ExpiresAtUtc),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : NoticeEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("trips:manage")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // FR-22: DRAFT → PUBLISHED.
        group.MapPost("/{id:guid}/publish", async (
            Guid id,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new PublishNoticeCommand(actor.Id, id), cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : NoticeEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("trips:manage")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // FR-22: take a notice off the board.
        group.MapPost("/{id:guid}/expire", async (
            Guid id,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new ExpireNoticeCommand(actor.Id, id), cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : NoticeEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("trips:manage")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    internal static IResult ErrorToProblem(Error error) =>
        VehicleEndpoints.ErrorToProblem(error);
}

public sealed record CreateNoticeRequest(
    string Title,
    string Body,
    NoticeAudience Audience,
    NoticeCategory Category,
    DateTime PublishAtUtc = default,
    DateTime? ExpiresAtUtc = null);

public sealed record UpdateNoticeRequest(
    string? Title,
    string? Body,
    NoticeAudience? Audience,
    NoticeCategory? Category,
    DateTime? PublishAtUtc,
    DateTime? ExpiresAtUtc);
