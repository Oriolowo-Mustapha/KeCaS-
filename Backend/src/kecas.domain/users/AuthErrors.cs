namespace kecas.domain.users;

public static class AuthErrors
{
    /// <summary>
    /// One message for unknown user and wrong password, so responses never
    /// reveal whether an account exists (design doc §8.2).
    /// </summary>
    public static readonly Error InvalidCredentials = new(
        "Auth.InvalidCredentials",
        "Invalid email or password.");

    public static readonly Error AccountLocked = new(
        "Auth.AccountLocked",
        "This account is temporarily locked after repeated failed sign-in attempts. Try again later.");

    public static readonly Error SessionInvalid = new(
        "Auth.SessionInvalid",
        "Your session is invalid or has expired. Sign in again.");

    public static readonly Error WeakPassword = new(
        "Auth.WeakPassword",
        "The new password is too weak. Use at least 10 characters including letters and digits.");

    public static readonly Error PermissionDenied = new(
        "Auth.PermissionDenied",
        "You do not have permission to perform this action.");
}
