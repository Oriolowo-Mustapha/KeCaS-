namespace kecas.domain.users;

/// <summary>
/// Raised when a user is deactivated. Infrastructure listens for this to
/// delete the user's sessions immediately (design doc §8.2).
/// </summary>
public sealed record UserDeactivatedDomainEvent(
    Guid UserId,
    Guid Id,
    DateTime OccurredAtUtc) : IDomainEvent;
