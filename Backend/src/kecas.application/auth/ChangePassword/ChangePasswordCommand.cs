namespace kecas.application.auth.ChangePassword;

/// <summary>
/// Change the signed-in user's own password (FR-01). The current session
/// (KeepTokenHash) stays valid; every other session of the user is revoked.
/// </summary>
public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string KeepTokenHash) : ICommand;
