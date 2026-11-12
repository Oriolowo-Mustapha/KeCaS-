namespace kecas.application.bookings.RejectBooking;

using System.Text.Json;
using kecas.application.abstractions.data;
using kecas.domain.bookings;

internal sealed class RejectBookingCommandHandler(
    IBookingRepository bookingRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RejectBookingCommand>
{
    public async Task<Result> Handle(RejectBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(command.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure(BookingErrors.NotFound);
        }

        var result = booking.Reject(command.ActorId, command.Reason);
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "booking.rejected",
            "booking",
            booking.Id.ToString(),
            before: JsonSerializer.Serialize(new { status = "REQUESTED" }),
            after: JsonSerializer.Serialize(new
            {
                status = booking.Status.ToStorageValue(),
                reason = booking.DecisionReason,
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
