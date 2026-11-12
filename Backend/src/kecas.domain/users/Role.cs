namespace kecas.domain.users;

/// <summary>
/// A named bundle of permissions (design decision DD-05: permission-based RBAC,
/// so "who approves fares" becomes a setting, not a code change).
/// </summary>
public sealed class Role : Entity
{
    private readonly List<Permission> _permissions = [];

    private Role(Guid id, string code, string name)
        : base(id)
    {
        Code = code;
        Name = name;
    }

    private Role()
    {
        // EF Core
    }

    /// <summary>Stable short code, e.g. MANAGER, OPERATOR, DRIVER, PASSENGER, ADMIN.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public IReadOnlyList<Permission> Permissions => _permissions.AsReadOnly();

    public static Result<Role> Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Role>(RoleErrors.CodeRequired);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Role>(RoleErrors.NameRequired);
        }

        var normalizedCode = code.Trim().ToUpperInvariant();

        if (!normalizedCode.All(char.IsLetter))
        {
            return Result.Failure<Role>(RoleErrors.CodeInvalid);
        }

        return new Role(Guid.NewGuid(), normalizedCode, name.Trim());
    }

    public bool HasPermission(string permissionCode) =>
        _permissions.Any(p =>
            string.Equals(p.Code, permissionCode, StringComparison.OrdinalIgnoreCase));

    public Result GrantPermission(Permission permission)
    {
        if (permission is null)
        {
            return Result.Failure(RoleErrors.PermissionRequired);
        }

        if (HasPermission(permission.Code))
        {
            return Result.Failure(RoleErrors.PermissionAlreadyGranted);
        }

        _permissions.Add(permission);
        return Result.Success();
    }

    public Result RevokePermission(string permissionCode)
    {
        var existing = _permissions.FirstOrDefault(p =>
            string.Equals(p.Code, permissionCode, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            return Result.Failure(RoleErrors.PermissionNotGranted);
        }

        _permissions.Remove(existing);
        return Result.Success();
    }
}
