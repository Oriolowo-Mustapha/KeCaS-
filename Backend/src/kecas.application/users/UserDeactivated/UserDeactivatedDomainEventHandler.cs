namespace kecas.application.users.UserDeactivated;

using kecas.application.abstractions.data;
using kecas.domain.users;
using MediatR;

/// <summary>
/// §8.2: deactivating an account immediately ends all of its sessions, so a
/// disabled user's cookie stops working on their very next request.
/// </summary>
internal sealed class UserDeactivatedDomainEventHandler(ISessionRepository sessionRepository)
    : INotificationHandler<UserDeactivatedDomainEvent>
{
    public Task Handle(UserDeactivatedDomainEvent notification, CancellationToken cancellationToken) =>
        sessionRepository.RevokeAllForUserAsync(
            notification.UserId,
            notification.OccurredAtUtc,
            cancellationToken);
}
