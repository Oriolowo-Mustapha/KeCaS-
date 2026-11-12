namespace kecas.application.users.DeactivateUser;

using System.Text.Json;
using kecas.application.abstractions.data;

internal sealed class DeactivateUserCommandHandler(
    IUserRepository userRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeactivateUserCommand>
{
    public async Task<Result> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound);
        }

        var before = JsonSerializer.Serialize(new { status = user.Status.ToString() });

        var result = user.Deactivate();
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "user.deactivated",
            "user",
            user.Id.ToString(),
            before,
            JsonSerializer.Serialize(new { status = user.Status.ToString() }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
