namespace kecas.application.auth.Logout;

/// <summary>
/// End the current session (FR-01). The API clears the cookie afterwards.
/// </summary>
public sealed record LogoutCommand(Guid UserId, string TokenHash) : ICommand;
