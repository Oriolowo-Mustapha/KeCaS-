namespace kecas.application.users.CreateUser;

using System.Text.Json;
using kecas.application.abstractions.authentication;
using kecas.application.abstractions.data;

internal sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher,
    IExistenceChecker existenceChecker,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<Guid>(emailResult.Error);
        }

        // NFR-07: email unique.
        if (await existenceChecker.UserEmailExistsAsync(emailResult.Value.Value, cancellationToken))
        {
            return Result.Failure<Guid>(UserErrors.EmailDuplicate);
        }

        var roles = new List<Role>();
        foreach (var roleCode in command.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var role = await roleRepository.GetByCodeAsync(roleCode.Trim().ToUpperInvariant(), cancellationToken);
            if (role is null)
            {
                return Result.Failure<Guid>(RoleErrors.NotFound);
            }

            roles.Add(role);
        }

        var userResult = User.Create(
            command.FullName,
            emailResult.Value.Value,
            passwordHasher.Hash(command.Password),
            command.Phone);
        if (userResult.IsFailure)
        {
            return Result.Failure<Guid>(userResult.Error);
        }

        var user = userResult.Value;
        foreach (var role in roles)
        {
            user.AssignRole(role);
        }

        userRepository.Add(user);

        auditWriter.Append(
            command.ActorId,
            "user.created",
            "user",
            user.Id.ToString(),
            before: null,
            after: JsonSerializer.Serialize(new
            {
                email = user.Email.Value,
                fullName = user.FullName,
                status = user.Status.ToString(),
                roles = roles.Select(r => r.Code).ToArray(),
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
