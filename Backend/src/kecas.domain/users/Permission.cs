namespace kecas.domain.users;

/// <summary>
/// A named capability checked on the server for every privileged action,
/// e.g. fare:approve, trips:create (design doc §8.1 permission matrix).
/// </summary>
public sealed class Permission : Entity
{
    private Permission(Guid id, string code, string description)
        : base(id)
    {
        Code = code;
        Description = description;
    }

    private Permission()
    {
        // EF Core
    }

    /// <summary>Stable short code, e.g. fares:approve, vehicles:create.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public static Result<Permission> Create(string code, string description = "")
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Permission>(PermissionErrors.CodeRequired);
        }

        var normalized = code.Trim().ToLowerInvariant();

        // Expected shape: area:action, e.g. fares:approve
        var colon = normalized.IndexOf(':');
        if (colon <= 0 || colon == normalized.Length - 1)
        {
            return Result.Failure<Permission>(PermissionErrors.CodeInvalid);
        }

        return new Permission(Guid.NewGuid(), normalized, description.Trim());
    }
}
