namespace kecas.infrastructure.Authentication;

using System.Security.Cryptography;
using kecas.application.abstractions.authentication;

/// <summary>
/// NFR-04: Argon2id password hashing (design doc §8.2). Produces PHC-formatted
/// strings ("$argon2id$v=19$m=65536,t=3,p=4$salt$hash"); nothing reversible
/// is stored. Verify uses a fixed-time comparison.
/// </summary>
internal sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 3;
    private const int MemorySize = 65536; // 64 MiB
    private const int DegreeOfParallelism = 4;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var hash = ComputeHash(password, salt, MemorySize, Iterations, DegreeOfParallelism);

        return
            $"$argon2id$v=19$m={MemorySize},t={Iterations},p={DegreeOfParallelism}"
            + $"${Convert.ToBase64String(salt)}"
            + $"${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string passwordHash, string password)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split('$', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 5 || !string.Equals(parts[0], "argon2id", StringComparison.Ordinal))
        {
            return false;
        }

        if (!parts[1].StartsWith("v=", StringComparison.Ordinal) || !int.TryParse(parts[1][2..], out _))
        {
            return false;
        }

        var parameters = parts[2].Split(',').Select(p => p.Split('='))
            .ToDictionary(kv => kv[0], kv => kv.Length == 2 ? int.Parse(kv[1]) : -1);

        if (parameters.Count != 3)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[3]);
        var expectedHash = Convert.FromBase64String(parts[4]);

        var actualHash = ComputeHash(
            password,
            salt,
            parameters["m"],
            parameters["t"],
            parameters["p"]);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static byte[] ComputeHash(
        string password,
        byte[] salt,
        int memorySize,
        int iterations,
        int degreeOfParallelism)
    {
        using var argon2 = new Konscious.Security.Cryptography.Argon2id(
            System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memorySize,
            Iterations = iterations,
            DegreeOfParallelism = degreeOfParallelism,
        };

        return argon2.GetBytes(HashSize);
    }
}
