namespace kecas.domain.users;

public static class RoleErrors
{
    public static readonly Error NotFound = new("Role.NotFound", "The specified role was not found.");

    public static readonly Error CodeRequired = new("Role.CodeRequired", "A role code is required.");

    public static readonly Error CodeInvalid = new(
        "Role.CodeInvalid",
        "A role code may only contain letters, e.g. MANAGER.");

    public static readonly Error NameRequired = new("Role.NameRequired", "A role name is required.");

    public static readonly Error PermissionRequired = new(
        "Role.PermissionRequired",
        "A permission is required.");

    public static readonly Error PermissionAlreadyGranted = new(
        "Role.PermissionAlreadyGranted",
        "The permission is already granted to this role.");

    public static readonly Error PermissionNotGranted = new(
        "Role.PermissionNotGranted",
        "The permission is not granted to this role.");
}
