namespace kecas.domain.users;

/// <summary>
/// Any signed-in user: staff (admin, manager, operator, driver) or passenger.
/// Passwords are stored as Argon2id hashes only; hashing itself lives in
/// Infrastructure — the Domain only ever receives the finished hash string.
/// </summary>
public sealed class User : Entity
{
    private readonly List<Role> _roles = [];

    private User(
        Guid id,
        string fullName,
        Email email,
        string? phone,
        string passwordHash,
        UserStatus status)
        : base(id)
    {
        FullName = fullName;
        Email = email;
        Phone = phone;
        PasswordHash = passwordHash;
        Status = status;
    }

    private User()
    {
        // EF Core
    }

    public string FullName { get; private set; } = string.Empty;

    public Email Email { get; private set; } = null!;

    public string? Phone { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public UserStatus Status { get; private set; }

    public DateTime? LastLoginAt { get; private set; }

    /// <summary>Failed sign-ins since the last success (§8.2 lockout policy).</summary>
    public int FailedLoginCount { get; private set; }

    public DateTime? LockoutUntil { get; private set; }

    /// <summary>§8.2: 5 failed attempts within 15 minutes locks the account.</summary>
    public const int LockoutThreshold = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public IReadOnlyList<Role> Roles => _roles.AsReadOnly();

    public bool IsLockedOut(DateTime nowUtc) => LockoutUntil.HasValue && LockoutUntil.Value > nowUtc;

    /// <summary>
    /// FR-02: the user's effective permissions, unioned across every role.
    /// </summary>
    public bool HasPermission(string permissionCode) =>
        _roles.Any(r => r.HasPermission(permissionCode));

    public static Result<User> Create(
        string fullName,
        string email,
        string passwordHash,
        string? phone = null)
    {
        var nameResult = ValidateFullName(fullName);
        if (!nameResult.IsSuccess)
        {
            return Result.Failure<User>(nameResult.Error);
        }

        var emailResult = Email.Create(email);
        if (!emailResult.IsSuccess)
        {
            return Result.Failure<User>(emailResult.Error);
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Result.Failure<User>(UserErrors.PasswordHashRequired);
        }

        var user = new User(
            Guid.NewGuid(),
            fullName.Trim(),
            emailResult.Value,
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            passwordHash,
            UserStatus.Active);

        return user;
    }

    public Result RecordSuccessfulLogin(DateTime occurredAtUtc)
    {
        if (Status == UserStatus.Disabled)
        {
            return Result.Failure(UserErrors.Disabled);
        }

        LastLoginAt = occurredAtUtc;
        FailedLoginCount = 0;
        LockoutUntil = null;
        return Result.Success();
    }

    public Result RecordFailedLogin(DateTime occurredAtUtc)
    {
        if (Status == UserStatus.Disabled)
        {
            return Result.Failure(UserErrors.Disabled);
        }

        FailedLoginCount++;

        if (FailedLoginCount >= LockoutThreshold)
        {
            LockoutUntil = occurredAtUtc.Add(LockoutDuration);
            FailedLoginCount = 0;
        }

        return Result.Success();
    }

    public Result ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            return Result.Failure(UserErrors.PasswordHashRequired);
        }

        if (Status == UserStatus.Disabled)
        {
            return Result.Failure(UserErrors.Disabled);
        }

        PasswordHash = newPasswordHash;
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (Status == UserStatus.Disabled)
        {
            return Result.Failure(UserErrors.AlreadyDisabled);
        }

        Status = UserStatus.Disabled;
        RaiseDomainEvent(new UserDeactivatedDomainEvent(Id, Guid.NewGuid(), DateTime.UtcNow));
        return Result.Success();
    }

    public Result Activate()
    {
        if (Status == UserStatus.Active)
        {
            return Result.Failure(UserErrors.AlreadyActive);
        }

        Status = UserStatus.Active;
        return Result.Success();
    }

    public Result AssignRole(Role role)
    {
        if (role is null)
        {
            return Result.Failure(UserErrors.RoleRequired);
        }

        if (_roles.Any(r => r.Id == role.Id))
        {
            return Result.Failure(UserErrors.RoleAlreadyAssigned);
        }

        _roles.Add(role);
        return Result.Success();
    }

    public Result RemoveRole(Role role)
    {
        if (role is null)
        {
            return Result.Failure(UserErrors.RoleRequired);
        }

        if (_roles.All(r => r.Id != role.Id))
        {
            return Result.Failure(UserErrors.RoleNotAssigned);
        }

        _roles.Remove(role);
        return Result.Success();
    }

    public Result UpdateProfile(string fullName, string? phone)
    {
        var nameResult = ValidateFullName(fullName);
        if (!nameResult.IsSuccess)
        {
            return Result.Failure(nameResult.Error);
        }

        FullName = fullName.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        return Result.Success();
    }

    private static Result ValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result.Failure(UserErrors.FullNameRequired);
        }

        if (fullName.Trim().Length > 120)
        {
            return Result.Failure(UserErrors.FullNameTooLong);
        }

        return Result.Success();
    }
}
