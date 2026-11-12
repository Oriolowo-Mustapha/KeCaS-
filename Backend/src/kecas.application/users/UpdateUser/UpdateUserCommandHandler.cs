namespace kecas.application.users.UpdateUser;

using System.Text.Json;
using kecas.application.abstractions.data;

internal sealed class UpdateUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateUserCommand>
{
    public async Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound);
        }

        var before = JsonSerializer.Serialize(new
        {
            fullName = user.FullName,
            phone = user.Phone,
            roles = user.Roles.Select(r => r.Code).ToArray(),
        });

        if (command.FullName is not null || command.Phone is not null)
        {
            var updateResult = user.UpdateProfile(
                command.FullName ?? user.FullName,
                command.Phone is null ? user.Phone : (string.IsNullOrEmpty(command.Phone) ? null : command.Phone));
            if (updateResult.IsFailure)
            {
                return updateResult;
            }
        }

        if (command.Roles is not null)
        {
            var requested = command.Roles
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(code => code.Trim().ToUpperInvariant())
                .ToList();

            var roles = await roleRepository.GetAllAsync(cancellationToken);
            var byCode = roles.ToDictionary(r => r.Code);

            foreach (var code in requested)
            {
                if (!byCode.TryGetValue(code, out var role))
                {
                    return Result.Failure(RoleErrors.NotFound);
                }

                if (user.Roles.All(r => r.Id != role.Id))
                {
                    user.AssignRole(role);
                }
            }

            foreach (var role in user.Roles.Where(r => !requested.Contains(r.Code)).ToList())
            {
                user.RemoveRole(role);
            }
        }

        auditWriter.Append(
            command.ActorId,
            "user.updated",
            "user",
            user.Id.ToString(),
            before,
            JsonSerializer.Serialize(new
            {
                fullName = user.FullName,
                phone = user.Phone,
                roles = user.Roles.Select(r => r.Code).ToArray(),
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
