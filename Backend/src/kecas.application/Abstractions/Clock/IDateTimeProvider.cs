namespace kecas.application.abstractions.clock;

/// <summary>
/// Time abstraction so domain logic and tests never call DateTime.UtcNow
/// directly. Implemented by DateTimeProvider in Infrastructure.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateOnly TodayUtc => DateOnly.FromDateTime(UtcNow);
}
