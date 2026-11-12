namespace kecas.application.auth.Login;

/// <summary>
/// The signed-in user plus the RAW session token. The token only ever travels
/// into the Set-Cookie header; the database stores its hash exclusively.
/// </summary>
public sealed record LoginResponse(
    Guid Id,
    string FullName,
    string Email,
    string[] Roles,
    string[] Permissions,
    string Token);
