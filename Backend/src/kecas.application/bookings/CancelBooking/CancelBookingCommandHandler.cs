namespace kecas.application.bookings.CancelBooking;

using System.Text.Json;
using kecas.application.abstractions.data;
using kecas.domain.bookings;

internal sealed class CancelBookingCommandHandler(
    IBookingRepository bookingRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelBookingCommand>
{
    private const string ManageBookingsPermission = "bookings:manage";

    public async Task<Result> Handle(CancelBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(command.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure(BookingErrors.NotFound);
        }

        // The passenger cancels their own request; staff may cancel any.
        if (booking.PassengerId != command.ActorId
            && !command.ActorPermissions.Contains(ManageBookingsPermission))
        {
            return Result.Failure(AuthErrors.PermissionDenied);
        }

        var result = booking.Cancel();
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "booking.cancelled",
            "booking",
            booking.Id.ToString(),
            before: JsonSerializer.Serialize(new { status = "REQUESTED" }),
            after: JsonSerializer.Serialize(new { status = booking.Status.ToStorageValue() }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
