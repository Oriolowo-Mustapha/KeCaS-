namespace kecas.domain.users;

public static class PermissionErrors
{
    public static readonly Error NotFound = new(
        "Permission.NotFound",
        "The specified permission was not found.");

    public static readonly Error CodeRequired = new(
        "Permission.CodeRequired",
        "A permission code is required.");

    public static readonly Error CodeInvalid = new(
        "Permission.CodeInvalid",
        "A permission code must look like area:action, e.g. fares:approve.");
}
