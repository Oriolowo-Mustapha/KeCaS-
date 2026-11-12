namespace kecas.application.auth.CurrentUser;

/// <summary>
/// The identity behind a live session: user info, effective roles and
/// permissions, plus the token hash so handlers can revoke exactly this
/// session (logout, password change). Never returned to the client.
/// </summary>
public sealed record SessionPrincipal(
    Guid Id,
    string FullName,
    string Email,
    string[] Roles,
    string[] Permissions,
    string TokenHash);
