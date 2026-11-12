namespace kecas.api.Endpoints;

using kecas.application.abstractions.messaging;
using kecas.application.feedback.ListFeedback;
using kecas.application.feedback.ReviewFeedback;
using kecas.application.feedback.SubmitFeedback;
using kecas.domain.abstractions;
using kecas.domain.feedback;
using MediatR;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Feedback endpoints (FR-21), behind FeatureFlags:Feedback. Submitting is
/// anonymous and needs no session; reviewing is bookings:manage staff.
/// </summary>
public static class FeedbackEndpoints
{
    public static IEndpointRouteBuilder MapFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/feedback")
            .WithTags("Feedback")
            .RequireFeature("Feedback");

        // FR-21: anonymous submission, minimal collection (no account needed).
        group.MapPost("", async (
            SubmitFeedbackRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new SubmitFeedbackCommand(
                    request.TripId,
                    request.Kind,
                    request.Message,
                    request.Rating,
                    request.Contact),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/feedback/{result.Value}", new { id = result.Value })
                : FeedbackEndpoints.ErrorToProblem(result.Error);
        })
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // FR-21: the staff triage board.
        group.MapGet("", async (
            ISender sender,
            CancellationToken cancellationToken,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? kind = null) =>
        {
            var result = await sender.Send(
                new ListFeedbackQuery(
                    page,
                    pageSize,
                    EnumQueryParser.Parse<FeedbackStatus>(status),
                    EnumQueryParser.Parse<FeedbackKind>(kind)),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : FeedbackEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("bookings:manage")
        .Produces(StatusCodes.Status200OK);

        // FR-21: staff triage, NEW/REVIEWED → REVIEWED/RESOLVED.
        group.MapPost("/{id:guid}/review", async (
            Guid id,
            ReviewFeedbackRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new ReviewFeedbackCommand(actor.Id, id, request.Target),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : FeedbackEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("bookings:manage")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    internal static IResult ErrorToProblem(Error error) =>
        VehicleEndpoints.ErrorToProblem(error);
}

public sealed record SubmitFeedbackRequest(
    Guid? TripId,
    FeedbackKind Kind,
    string Message,
    int? Rating,
    string? Contact);

public sealed record ReviewFeedbackRequest(FeedbackStatus Target);
