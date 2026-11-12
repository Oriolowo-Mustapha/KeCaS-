namespace kecas.api.Endpoints;

using kecas.application.abstractions.messaging;
using kecas.application.bookings.CancelBooking;
using kecas.application.bookings.CompleteBooking;
using kecas.application.bookings.ConfirmBooking;
using kecas.application.bookings.CreateBooking;
using kecas.application.bookings.ListBookings;
using kecas.application.bookings.NoShowBooking;
using kecas.application.bookings.RejectBooking;
using kecas.domain.abstractions;
using kecas.domain.bookings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Booking endpoints (FR-16/17, DD-09), behind FeatureFlags:Booking. Passengers
/// request and cancel their own bookings; bookings:manage staff confirm,
/// reject, complete and mark no-shows (§7.4).
/// </summary>
public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/bookings")
            .WithTags("Bookings")
            .RequireFeature("Booking");

        // FR-16: the signed-in passenger requests seats on a future trip.
        group.MapPost("", async (
            CreateBookingRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new CreateBookingCommand(
                    actor.Id,
                    request.TripId,
                    request.PickupStopId,
                    request.DropoffStopId,
                    request.PartySize),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/bookings/{result.Value.BookingId}", result.Value)
                : BookingEndpoints.ErrorToProblem(result.Error);
        })
        .RequireSession()
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // FR-17: the staff request board (passengers use /mine).
        group.MapGet("", async (
            ISender sender,
            CancellationToken cancellationToken,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] Guid? passengerId = null,
            [FromQuery] Guid? tripId = null) =>
        {
            var result = await sender.Send(
                new ListBookingsQuery(
                    page,
                    pageSize,
                    EnumQueryParser.Parse<BookingStatus>(status),
                    passengerId,
                    tripId),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : BookingEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("bookings:manage")
        .Produces(StatusCodes.Status200OK);

        // FR-16: the signed-in passenger's own bookings.
        group.MapGet("/mine", async (
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new ListBookingsQuery(
                    page,
                    pageSize,
                    EnumQueryParser.Parse<BookingStatus>(status),
                    actor.Id,
                    null),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : BookingEndpoints.ErrorToProblem(result.Error);
        })
        .RequireSession()
        .Produces(StatusCodes.Status200OK);

        // FR-17 / §7.4 step 4: lock the trip row, check BR-09 capacity, confirm.
        group.MapPost("/{id:guid}/confirm", async (
            Guid id,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new ConfirmBookingCommand(actor.Id, id), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : BookingEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("bookings:manage")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // FR-17: REQUESTED → REJECTED (+ reason).
        group.MapPost("/{id:guid}/reject", async (
            Guid id,
            RejectBookingRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new RejectBookingCommand(actor.Id, id, request.Reason), cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : BookingEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("bookings:manage")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // FR-16: passenger withdraws an undecided request (staff may too).
        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new CancelBookingCommand(actor.Id, actor.Permissions, id),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : BookingEndpoints.ErrorToProblem(result.Error);
        })
        .RequireSession()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // FR-17: CONFIRMED → COMPLETED (the trip ran with the passenger).
        group.MapPost("/{id:guid}/complete", async (
            Guid id,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new CompleteBookingCommand(actor.Id, id), cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : BookingEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("bookings:manage")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // FR-17: the confirmed passenger never showed.
        group.MapPost("/{id:guid}/no-show", async (
            Guid id,
            NoShowBookingRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new NoShowBookingCommand(actor.Id, id, request.Reason), cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : BookingEndpoints.ErrorToProblem(result.Error);
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

public sealed record CreateBookingRequest(
    Guid TripId,
    Guid? PickupStopId,
    Guid? DropoffStopId,
    int PartySize);

public sealed record RejectBookingRequest(string? Reason);

public sealed record NoShowBookingRequest(string? Reason);
