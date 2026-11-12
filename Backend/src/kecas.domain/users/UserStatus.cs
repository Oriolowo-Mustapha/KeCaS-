namespace kecas.domain.users;

/// <summary>
/// Account status for any signed-in user (design doc §4.2: ACTIVE or DISABLED).
/// Deactivation keeps the row so old trips and audit entries still make sense.
/// </summary>
public enum UserStatus
{
    Active = 1,
    Disabled = 2,
}
