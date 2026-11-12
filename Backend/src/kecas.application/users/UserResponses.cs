namespace kecas.application.users;

/// <summary>Shared response shapes for the user-admin module (FR-03).</summary>
public sealed record UserListItem(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string Status,
    string[] Roles,
    DateTime? LastLoginAt);

public sealed record ListUsersResponse(IReadOnlyList<UserListItem> Items, int Page, int PageSize, int Total);
