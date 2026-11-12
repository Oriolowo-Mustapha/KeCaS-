namespace kecas.application.users.CreateUser;

/// <summary>
/// Admin creates an account (FR-03 / UC-08). The admin supplies the initial
/// password; the user changes it after first sign-in. Unknown role codes are
/// rejected rather than silently skipped.
/// </summary>
public sealed record CreateUserCommand(
    Guid ActorId,
    string FullName,
    string Email,
    string? Phone,
    string Password,
    string[] Roles) : ICommand<Guid>;
