namespace kecas.application.users.ListUsers;

using kecas.application.abstractions.data;

internal sealed class ListUsersQueryHandler(IUserRepository userRepository)
    : IQueryHandler<ListUsersQuery, ListUsersResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<ListUsersResponse>> Handle(
        ListUsersQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var users = await userRepository.ListAsync(page, pageSize, cancellationToken);
        var total = await userRepository.CountAsync(cancellationToken);

        var items = users.Select(u => new UserListItem(
            u.Id,
            u.FullName,
            u.Email.Value,
            u.Phone,
            u.Status.ToString(),
            [.. u.Roles.Select(r => r.Code)],
            u.LastLoginAt)).ToList();

        return Result.Success(new ListUsersResponse(items, page, pageSize, total));
    }
}
