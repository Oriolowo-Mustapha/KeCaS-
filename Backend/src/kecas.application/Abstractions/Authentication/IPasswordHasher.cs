namespace kecas.application.abstractions.authentication;

/// <summary>
/// NFR-04: passwords are stored with an established password-hashing algorithm
/// (Argon2id) and never as plain text. The Domain only ever receives the
/// finished hash string.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}
