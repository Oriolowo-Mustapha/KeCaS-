namespace kecas.application.auth.CurrentUser;

/// <summary>
/// Validates the session cookie sent with a request (FR-01 / FR-02) and, when
/// RequiredPermission is set, enforces the server-side permission check.
/// Returns the signed-in user's identity for endpoint handlers to use.
/// </summary>
public sealed record ValidateSessionQuery(string? Token, string? RequiredPermission) : IQuery<SessionPrincipal>;
