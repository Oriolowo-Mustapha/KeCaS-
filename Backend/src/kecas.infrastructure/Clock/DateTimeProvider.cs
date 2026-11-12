namespace kecas.infrastructure.Clock;

using kecas.application.abstractions.clock;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
