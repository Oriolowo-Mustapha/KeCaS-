using kecas.application;
using kecas.application.abstractions.messaging;
using kecas.application.fleet.ChangeVehicleStatus;
using kecas.application.fleet.CreateVehicle;
using kecas.application.fleet.GetVehicle;
using kecas.application.fleet.ListVehicles;
using kecas.application.fleet.UpdateVehicle;
using kecas.domain.abstractions;
using kecas.domain.common;
using kecas.domain.fleet;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace kecas.api.Endpoints;

public static class VehicleEndpoints
{
    public static IEndpointRouteBuilder MapVehicleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/vehicles")
            .WithTags("Vehicles");

        // FR-12 conventions: page/page_size, filters (category, status,
        // operator). Reads need vehicles:view (§8.1).
        group.MapGet("", async (
            ISender sender,
            int? page,
            int? pageSize,
            string? category,
            string? status,
            Guid? operatorId,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListVehiclesQuery(
                    page ?? 1,
                    pageSize ?? 20,
                    EnumQueryParser.Parse<VehicleCategory>(category),
                    EnumQueryParser.Parse<VehicleStatus>(status),
                    operatorId),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ErrorToProblem(result.Error);
        })
        .RequirePermission("vehicles:view")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetVehicleQuery(id), cancellationToken);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ErrorToProblem(result.Error);
        })
        .RequirePermission("vehicles:view")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // FR-04 / UC-02, enforced by FR-02: registering a vehicle needs a
        // live session with the vehicles:manage permission (§8.1).
        group.MapPost("", async (
            CreateVehicleCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/vehicles/{result.Value}", new { id = result.Value })
                : ErrorToProblem(result.Error);
        })
        .RequirePermission("vehicles:manage")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status404NotFound);

        // FR-04: PATCH /vehicles/:id (capacity edits; identity is immutable).
        group.MapPatch("/{id:guid}", async (
            Guid id,
            UpdateVehicleRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new UpdateVehicleCommand(actor.Id, id, request.Capacity),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : ErrorToProblem(result.Error);
        })
        .RequirePermission("vehicles:manage")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // FR-05: status change with a mandatory reason (§5.3).
        group.MapPost("/{id:guid}/status", async (
            Guid id,
            ChangeVehicleStatusRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new ChangeVehicleStatusCommand(actor.Id, id, request.Status, request.Reason),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : ErrorToProblem(result.Error);
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

    /// <summary>
    /// Maps domain errors to the API error envelope from design doc §6.1
    /// (400 validation, 401 not signed in, 403 not permitted, 404 not found,
    /// 409 duplicate/conflict, 422 business rule broken, 429 rate limited).
    /// </summary>
    internal static IResult ErrorToProblem(Error error)
    {
        var status = error.Code switch
        {
            var code when code.EndsWith("NotFound", StringComparison.Ordinal) => StatusCodes.Status404NotFound,
            var code when code.EndsWith("Duplicate", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
            var code when code.EndsWith("Exists", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
            "Auth.InvalidCredentials" or "Auth.SessionInvalid" => StatusCodes.Status401Unauthorized,
            "Auth.AccountLocked" => StatusCodes.Status429TooManyRequests,
            "Auth.PermissionDenied" => StatusCodes.Status403Forbidden,
            // §5.3: the approver must differ from the proposer.
            "Fare.SelfApproval" => StatusCodes.Status403Forbidden,
            // §7.3: clashing trip → 409 TRIP_OVERLAP; stale row_version → 409.
            "Trip.TripOverlap" or "Trip.RowVersionConflict" => StatusCodes.Status409Conflict,
            // §4.2: the driver/vehicle already has an open shift.
            "Shift.AlreadyOpen" or "Shift.VehicleAlreadyOnShift" => StatusCodes.Status409Conflict,
            // §7.4 step 4 / BR-09: confirming would exceed the vehicle capacity.
            "Booking.CapacityExceeded" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        return Results.Problem(
            title: error.Description,
            detail: error.Description,
            statusCode: status,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
            });
    }
}

public sealed record UpdateVehicleRequest(int? Capacity);

public sealed record ChangeVehicleStatusRequest(VehicleStatus Status, string Reason);

/// <summary>
/// Translates validation failures (from the pipeline) and unhandled
/// exceptions into ProblemDetails with field-level errors (NFR-06, §6.1).
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // FluentValidation failures (ValidationBehavior) → 400 with the
        // field-level messages (§6.1 validation; NFR-06).
        if (exception is ValidationException validation)
        {
            // Class-level rules carry no property name; an empty key would be
            // ugly (and unparseable for some JSON parsers), so use "_".
            var fields = validation.Errors
                .Where(failure => failure is not null)
                .GroupBy(
                    failure => string.IsNullOrEmpty(failure.PropertyName)
                        ? "_"
                        : failure.PropertyName,
                    failure => failure.ErrorMessage,
                    (name, messages) => (name, messages))
                .ToDictionary(pair => pair.name, pair => pair.messages.ToArray());

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed.",
                    Extensions = new Dictionary<string, object?>
                    {
                        ["code"] = "Validation.Failed",
                        ["fields"] = fields,
                    },
                },
            });
        }

        logger.LogError(exception, "Unhandled exception for {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
            },
        });
    }
}
