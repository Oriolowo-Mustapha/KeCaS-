namespace kecas.application.users.DeactivateUser;

/// <summary>
/// POST /users/:id/deactivate (FR-03 / UC-08). Raising the domain event also
/// ends every session of the account — the deactivation handler revokes them
/// after the save (design doc §8.2 / TC-USR-02).
/// </summary>
public sealed record DeactivateUserCommand(Guid ActorId, Guid UserId) : ICommand;
