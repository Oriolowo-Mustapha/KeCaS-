namespace kecas.api.Endpoints;

using kecas.application.abstractions.messaging;
using kecas.application.payments.ListPayments;
using kecas.application.payments.RecordPayment;
using kecas.domain.abstractions;
using kecas.domain.payments;
using MediatR;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Payment record endpoints (FR-18, DD-08), behind FeatureFlags:Payments.
/// Records only — the money moves outside the system (cash, transfer, POS).
/// </summary>
public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payments")
            .WithTags("Payments")
            .RequireFeature("Payments");

        // FR-18: record a payment that happened outside the system.
        group.MapPost("", async (
            RecordPaymentRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var actor = RequirePermissionFilter.Principal(http);
            var result = await sender.Send(
                new RecordPaymentCommand(
                    actor.Id,
                    request.BookingId,
                    request.TripId,
                    request.AmountKobo,
                    request.Method,
                    request.Status,
                    request.Reference),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/payments/{result.Value}", new { id = result.Value })
                : PaymentEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("payments:record")
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // FR-18: the payment ledger with filters.
        group.MapGet("", async (
            ISender sender,
            CancellationToken cancellationToken,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] Guid? bookingId = null,
            [FromQuery] Guid? tripId = null) =>
        {
            var result = await sender.Send(
                new ListPaymentsQuery(
                    page,
                    pageSize,
                    EnumQueryParser.Parse<PaymentStatus>(status),
                    bookingId,
                    tripId),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : PaymentEndpoints.ErrorToProblem(result.Error);
        })
        .RequirePermission("payments:record")
        .Produces(StatusCodes.Status200OK);

        return app;
    }

    internal static IResult ErrorToProblem(Error error) =>
        VehicleEndpoints.ErrorToProblem(error);
}

public sealed record RecordPaymentRequest(
    Guid? BookingId,
    Guid? TripId,
    int AmountKobo,
    PaymentMethod Method,
    PaymentStatus Status,
    string? Reference);
