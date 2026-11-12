namespace kecas.application.payments.RecordPayment;

using System.Text.Json;
using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;
using kecas.domain.bookings;
using kecas.domain.common;
using kecas.domain.payments;
using kecas.domain.trips;

internal sealed class RecordPaymentCommandHandler(
    IPaymentRepository paymentRepository,
    IBookingRepository bookingRepository,
    ITripRepository tripRepository,
    IDateTimeProvider dateTimeProvider,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RecordPaymentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RecordPaymentCommand command, CancellationToken cancellationToken)
    {
        // DD-08: the record must point at a real booking or trip.
        if (command.BookingId is { } bookingId
            && await bookingRepository.GetByIdAsync(bookingId, cancellationToken) is null)
        {
            return Result.Failure<Guid>(PaymentErrors.BookingNotFound);
        }

        if (command.TripId is { } tripId
            && await tripRepository.GetByIdAsync(tripId, cancellationToken) is null)
        {
            return Result.Failure<Guid>(PaymentErrors.TripNotFound);
        }

        var created = Payment.Create(
            command.BookingId,
            command.TripId,
            new Money(command.AmountKobo),
            command.Method,
            command.Status,
            command.Reference,
            command.ActorId,
            dateTimeProvider.UtcNow);
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        paymentRepository.Add(created.Value);

        auditWriter.Append(
            command.ActorId,
            "payment.recorded",
            "payment",
            created.Value.Id.ToString(),
            after: JsonSerializer.Serialize(new
            {
                bookingId = created.Value.BookingId,
                tripId = created.Value.TripId,
                amountKobo = created.Value.Amount.AmountInKobo,
                method = created.Value.Method.ToStorageValue(),
                status = created.Value.Status.ToStorageValue(),
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(created.Value.Id);
    }
}
