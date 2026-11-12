namespace kecas.application.bookings.CompleteBooking;

using System.Text.Json;
using kecas.application.abstractions.data;
using kecas.domain.bookings;

internal sealed class CompleteBookingCommandHandler(
    IBookingRepository bookingRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CompleteBookingCommand>
{
    public async Task<Result> Handle(CompleteBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(command.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure(BookingErrors.NotFound);
        }

        var result = booking.Complete(command.ActorId);
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "booking.completed",
            "booking",
            booking.Id.ToString(),
            before: JsonSerializer.Serialize(new { status = "CONFIRMED" }),
            after: JsonSerializer.Serialize(new { status = booking.Status.ToStorageValue() }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
