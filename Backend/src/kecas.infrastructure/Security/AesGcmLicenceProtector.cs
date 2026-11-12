namespace kecas.infrastructure.Security;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// NFR-05 / §8.2: driver licence numbers are encrypted at rest with
/// AES-256-GCM (authenticated encryption). Storage format:
/// "enc1:" + base64(nonce(12) || ciphertext || tag(16)).
/// The key comes from Security__Licence_Key in the environment (the .env
/// file is loaded into environment variables at startup) and is never
/// committed. Values without the prefix are legacy plaintext and pass
/// through unchanged on read, so existing rows stay readable until the
/// `encrypt-licences` command rewrites them.
/// </summary>
internal sealed class AesGcmLicenceProtector
{
    private const string Prefix = "enc1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] key;

    public AesGcmLicenceProtector()
    {
        var base64Key = Environment.GetEnvironmentVariable("Security__Licence_Key");
        if (string.IsNullOrWhiteSpace(base64Key))
        {
            throw new InvalidOperationException(
                "Licence encryption key is missing. Set Security__Licence_Key "
                + "(a 32-byte key as base64) in the .env file.");
        }

        key = Convert.FromBase64String(base64Key);
        if (key.Length != 32)
        {
            throw new InvalidOperationException(
                "Security__Licence_Key must decode to exactly 32 bytes for AES-256.");
        }
    }

    /// <summary>Randomly generates a fresh 256-bit encryption key as base64.</summary>
    public static string NewKey() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public string Encrypt(string plaintext)
    {
        // Already encrypted rows are never double-encrypted.
        if (plaintext.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return plaintext;
        }

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[Encoding.UTF8.GetByteCount(plaintext)];
        var tag = new byte[TagSize];
        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Encrypt(nonce, Encoding.UTF8.GetBytes(plaintext), ciphertext, tag);
        }

        var payload = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, payload, NonceSize + ciphertext.Length, TagSize);
        return Prefix + Convert.ToBase64String(payload);
    }

    public string Decrypt(string stored)
    {
        // Legacy plaintext rows (written before this hardening pass) decrypt
        // to themselves; the encrypt-licences command upgrades them in place.
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return stored;
        }

        var payload = Convert.FromBase64String(stored[Prefix.Length..]);
        var nonce = payload[..NonceSize];
        var ciphertext = payload[NonceSize..^TagSize];
        var tag = payload[^TagSize..];
        var plaintext = new byte[ciphertext.Length];
        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        return Encoding.UTF8.GetString(plaintext);
    }
}
