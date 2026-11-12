namespace kecas.application.auth.Login;

/// <summary>
/// Sign in with email and password (FR-01). On success the handler issues a
/// session and returns its raw token so the API can set the session cookie.
/// </summary>
public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResponse>;
