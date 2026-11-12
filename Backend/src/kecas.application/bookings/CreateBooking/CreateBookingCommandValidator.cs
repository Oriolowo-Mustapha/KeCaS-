namespace kecas.application.bookings.CreateBooking;

using FluentValidation;

internal sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(c => c.ActorId).NotEmpty();

        RuleFor(c => c.TripId).NotEmpty();

        RuleFor(c => c.PartySize)
            .InclusiveBetween(1, 20)
            .WithMessage("Party size must be between 1 and 20.");
    }
}
