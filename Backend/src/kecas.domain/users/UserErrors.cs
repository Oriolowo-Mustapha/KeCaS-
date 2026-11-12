namespace kecas.domain.users;

public static class UserErrors
{
    public static readonly Error NotFound = new("User.NotFound", "The specified user was not found.");

    public static readonly Error EmailDuplicate = new(
        "User.EmailDuplicate",
        "An account with this email already exists.");

    public static readonly Error FullNameRequired = new(
        "User.FullNameRequired",
        "A full name is required.");

    public static readonly Error FullNameTooLong = new(
        "User.FullNameTooLong",
        "The full name may not exceed 120 characters.");

    public static readonly Error PasswordHashRequired = new(
        "User.PasswordHashRequired",
        "A password hash is required.");

    public static readonly Error Disabled = new(
        "User.Disabled",
        "This account is disabled and cannot be used.");

    public static readonly Error AlreadyDisabled = new(
        "User.AlreadyDisabled",
        "The user account is already disabled.");

    public static readonly Error AlreadyActive = new(
        "User.AlreadyActive",
        "The user account is already active.");

    public static readonly Error RoleRequired = new(
        "User.RoleRequired",
        "A role is required.");

    public static readonly Error RoleAlreadyAssigned = new(
        "User.RoleAlreadyAssigned",
        "The role is already assigned to this user.");

    public static readonly Error RoleNotAssigned = new(
        "User.RoleNotAssigned",
        "The role is not assigned to this user.");
}
