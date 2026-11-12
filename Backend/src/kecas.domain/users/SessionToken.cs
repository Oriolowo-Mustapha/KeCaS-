namespace kecas.domain.users;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// A session token. The RAW value travels in the session cookie and is never
/// stored; only its SHA-256 hash is persisted, so a database leak cannot be
/// replayed as a login (design doc §8.2: stolen/stale session controls).
/// </summary>
public sealed record SessionToken
{
    private SessionToken(string raw, string hash)
    {
        Raw = raw;
        Hash = hash;
    }

    /// <summary>The opaque cookie value. Never store this.</summary>
    public string Raw { get; }

    /// <summary>Hex SHA-256 of the raw token — the only value stored.</summary>
    public string Hash { get; }

    public static SessionToken New()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Base64UrlEncode(bytes);
        return new SessionToken(raw, HashOf(raw));
    }

    public static Result<SessionToken> FromRaw(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length is < 40 or > 128)
        {
            return Result.Failure<SessionToken>(new Error(
                "SessionToken.Invalid",
                "The session token is malformed."));
        }

        return new SessionToken(raw, HashOf(raw));
    }

    private static string HashOf(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
