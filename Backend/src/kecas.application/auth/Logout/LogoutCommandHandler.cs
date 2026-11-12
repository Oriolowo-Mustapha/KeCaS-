namespace kecas.application.auth.Logout;

using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;

internal sealed class LogoutCommandHandler(
    ISessionRepository sessionRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByTokenHashAsync(command.TokenHash, cancellationToken);

        // Logout is idempotent: an unknown or already-expired session still
        // results in a signed-out user.
        if (session is not null && session.UserId == command.UserId)
        {
            session.Revoke(dateTimeProvider.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
