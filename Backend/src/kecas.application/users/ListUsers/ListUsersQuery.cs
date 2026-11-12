namespace kecas.application.users.ListUsers;

/// <summary>List accounts for administration (FR-03). Admin only.</summary>
public sealed record ListUsersQuery(int Page = 1, int PageSize = 20) : IQuery<ListUsersResponse>;
