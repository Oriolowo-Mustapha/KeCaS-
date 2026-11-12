namespace kecas.application.users.UpdateUser;

/// <summary>
/// PATCH semantics (FR-03): fields left null are unchanged. Roles, when given,
/// replace the current set in one go. Code/plate-like immutable data (email)
/// is not editable here — accounts keep a stable identity.
/// </summary>
public sealed record UpdateUserCommand(
    Guid ActorId,
    Guid UserId,
    string? FullName,
    string? Phone,
    string[]? Roles) : ICommand;
