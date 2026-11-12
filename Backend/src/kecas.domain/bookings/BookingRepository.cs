namespace kecas.domain.bookings;

public static class BookingErrors
{
    public static readonly Error NotFound = new(
        "Booking.NotFound",
        "The specified booking was not found.");

    public static readonly Error CodeDuplicate = new(
        "Booking.CodeDuplicate",
        "A booking with this code already exists.");

    public static readonly Error ScopeInvalid = new(
        "Booking.ScopeInvalid",
        "A booking needs a trip and a passenger.");

    public static readonly Error PartySizeInvalid = new(
        "Booking.PartySizeInvalid",
        "Party size must be between 1 and 20.");

    public static readonly Error TripNotBookable = new(
        "Booking.TripNotBookable",
        "Bookings can only be requested on future scheduled trips.");

    public static readonly Error StopNotFound = new(
        "Booking.StopNotFound",
        "The specified pickup or drop-off stop was not found.");

    public static readonly Error NotConfirmable = new(
        "Booking.NotConfirmable",
        "Only requested bookings can be confirmed.");

    public static readonly Error NotDecidable = new(
        "Booking.NotDecidable",
        "This booking is no longer awaiting a decision.");

    public static readonly Error NotCancellable = new(
        "Booking.NotCancellable",
        "Only requested bookings can be cancelled.");

    public static readonly Error NotCompletable = new(
        "Booking.NotCompletable",
        "Only confirmed bookings can be completed.");

    public static readonly Error CapacityExceeded = new(
        "Booking.CapacityExceeded",
        "Confirming this booking would exceed the vehicle's capacity.");

    public static readonly Error PermissionDenied = new(
        "Booking.PermissionDenied",
        "You can only manage your own bookings.");
}

/// <summary>Repository for the Booking aggregate (FR-16/17).</summary>
public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Booking?> GetByCodeAsync(string bookingCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> ListAsync(
        int page,
        int pageSize,
        BookingStatus? status = null,
        Guid? passengerId = null,
        Guid? tripId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        BookingStatus? status = null,
        Guid? passengerId = null,
        Guid? tripId = null,
        CancellationToken cancellationToken = default);

    /// <summary>BR-09: total confirmed party size on a trip, read inside the confirm transaction.</summary>
    Task<int> SumConfirmedPartySizeAsync(Guid tripId, CancellationToken cancellationToken = default);

    void Add(Booking booking);
}
