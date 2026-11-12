namespace kecas.application.bookings.NoShowBooking;

using System.Text.Json;
using kecas.application.abstractions.data;
using kecas.domain.bookings;

internal sealed class NoShowBookingCommandHandler(
    IBookingRepository bookingRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<NoShowBookingCommand>
{
    public async Task<Result> Handle(NoShowBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(command.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure(BookingErrors.NotFound);
        }

        var result = booking.MarkNoShow(command.ActorId, command.Reason);
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "booking.no_show",
            "booking",
            booking.Id.ToString(),
            before: JsonSerializer.Serialize(new { status = "CONFIRMED" }),
            after: JsonSerializer.Serialize(new
            {
                status = booking.Status.ToStorageValue(),
                reason = booking.DecisionReason,
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
