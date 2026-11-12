namespace kecas.domain.bookings;

using kecas.domain.abstractions;

/// <summary>
/// A passenger's seat request on a scheduled trip (FR-16/17, R3). Stores a
/// user reference and party size only — no extra personal details (§4.2).
/// </summary>
public sealed class Booking : Entity
{
    private Booking(
        Guid id,
        string bookingCode,
        Guid passengerId,
        Guid tripId,
        Guid? pickupStopId,
        Guid? dropoffStopId,
        int partySize,
        BookingStatus status,
        DateTime createdAtUtc)
        : base(id)
    {
        BookingCode = bookingCode;
        PassengerId = passengerId;
        TripId = tripId;
        PickupStopId = pickupStopId;
        DropoffStopId = dropoffStopId;
        PartySize = partySize;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    private Booking()
    {
        // EF Core
    }

    /// <summary>BR-01: unique human-facing code, e.g. BKG-3F91A2C7.</summary>
    public string BookingCode { get; private set; } = null!;

    public Guid PassengerId { get; private set; }

    public Guid TripId { get; private set; }

    public Guid? PickupStopId { get; private set; }

    public Guid? DropoffStopId { get; private set; }

    public int PartySize { get; private set; }

    public BookingStatus Status { get; private set; }

    /// <summary>Generated when the booking is CONFIRMED; shown to the driver.</summary>
    public string? RideCode { get; private set; }

    public Guid? DecidedBy { get; private set; }

    public string? DecisionReason { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Result<Booking> Request(
        Guid tripId,
        Guid passengerId,
        Guid? pickupStopId,
        Guid? dropoffStopId,
        int partySize,
        DateTime createdAtUtc,
        string bookingCode)
    {
        if (tripId == Guid.Empty || passengerId == Guid.Empty)
        {
            return Result.Failure<Booking>(BookingErrors.ScopeInvalid);
        }

        if (partySize < 1 || partySize > 20)
        {
            return Result.Failure<Booking>(BookingErrors.PartySizeInvalid);
        }

        if (string.IsNullOrWhiteSpace(bookingCode))
        {
            return Result.Failure<Booking>(BookingErrors.ScopeInvalid);
        }

        return new Booking(
            Guid.NewGuid(),
            bookingCode,
            passengerId,
            tripId,
            pickupStopId is { } p && p != Guid.Empty ? p : null,
            dropoffStopId is { } d && d != Guid.Empty ? d : null,
            partySize,
            BookingStatus.Requested,
            createdAtUtc);
    }

    /// <summary>§7.4 step 4: capacity is checked by the handler inside a transaction.</summary>
    public Result Confirm(Guid deciderId, string rideCode)
    {
        if (Status != BookingStatus.Requested)
        {
            return Result.Failure(BookingErrors.NotConfirmable);
        }

        if (deciderId == Guid.Empty || string.IsNullOrWhiteSpace(rideCode))
        {
            return Result.Failure(BookingErrors.NotConfirmable);
        }

        Status = BookingStatus.Confirmed;
        RideCode = rideCode;
        DecidedBy = deciderId;
        return Result.Success();
    }

    public Result Reject(Guid deciderId, string? reason)
    {
        if (Status != BookingStatus.Requested)
        {
            return Result.Failure(BookingErrors.NotDecidable);
        }

        if (deciderId == Guid.Empty)
        {
            return Result.Failure(BookingErrors.NotDecidable);
        }

        Status = BookingStatus.Rejected;
        DecidedBy = deciderId;
        DecisionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        return Result.Success();
    }

    /// <summary>The passenger cancels their own request while it is undecided.</summary>
    public Result Cancel()
    {
        if (Status != BookingStatus.Requested)
        {
            return Result.Failure(BookingErrors.NotCancellable);
        }

        Status = BookingStatus.Cancelled;
        return Result.Success();
    }

    /// <summary>The trip was completed with the passenger aboard.</summary>
    public Result Complete(Guid deciderId)
    {
        if (Status != BookingStatus.Confirmed)
        {
            return Result.Failure(BookingErrors.NotCompletable);
        }

        Status = BookingStatus.Completed;
        DecidedBy = deciderId;
        return Result.Success();
    }

    /// <summary>The confirmed passenger never showed up (manager).</summary>
    public Result MarkNoShow(Guid deciderId, string? reason)
    {
        if (Status != BookingStatus.Confirmed)
        {
            return Result.Failure(BookingErrors.NotDecidable);
        }

        Status = BookingStatus.NoShow;
        DecidedBy = deciderId;
        DecisionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        return Result.Success();
    }
}
