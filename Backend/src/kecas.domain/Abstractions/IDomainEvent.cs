namespace kecas.domain.abstractions;

using MediatR;

/// <summary>
/// Marker for domain events raised by aggregates. Inherits MediatR's
/// INotification — the one approved external dependency in Domain — so the
/// application layer can publish them after saving.
/// </summary>
public interface IDomainEvent : INotification
{
    Guid Id { get; }

    DateTime OccurredAtUtc { get; }
}
