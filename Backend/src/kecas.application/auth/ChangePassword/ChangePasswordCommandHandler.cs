namespace kecas.application.auth.ChangePassword;

using kecas.application.abstractions.authentication;
using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;

internal sealed class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;

        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null || user.Status == UserStatus.Disabled)
        {
            return Result.Failure(AuthErrors.SessionInvalid);
        }

        if (!passwordHasher.Verify(user.PasswordHash, command.CurrentPassword))
        {
            return Result.Failure(AuthErrors.InvalidCredentials);
        }

        var changeResult = user.ChangePassword(passwordHasher.Hash(command.NewPassword));
        if (changeResult.IsFailure)
        {
            return changeResult;
        }

        // §8.2: other sessions of this user do not survive a password change.
        await sessionRepository.RevokeAllForUserExceptAsync(
            user.Id, command.KeepTokenHash, now, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
