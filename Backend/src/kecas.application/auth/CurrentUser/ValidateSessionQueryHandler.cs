namespace kecas.application.auth.CurrentUser;

using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;

internal sealed class ValidateSessionQueryHandler(
    ISessionRepository sessionRepository,
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IQueryHandler<ValidateSessionQuery, SessionPrincipal>
{
    public async Task<Result<SessionPrincipal>> Handle(
        ValidateSessionQuery query,
        CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;

        var tokenResult = SessionToken.FromRaw(query.Token);
        if (tokenResult.IsFailure)
        {
            return Result.Failure<SessionPrincipal>(AuthErrors.SessionInvalid);
        }

        var session = await sessionRepository.GetByTokenHashAsync(tokenResult.Value.Hash, cancellationToken);
        if (session is null || !session.IsAlive(now))
        {
            return Result.Failure<SessionPrincipal>(AuthErrors.SessionInvalid);
        }

        // Sliding window: every authenticated request extends the 8h idle limit.
        session.Touch(now);

        var user = await userRepository.GetByIdAsync(session.UserId, cancellationToken);
        if (user is null || user.Status == UserStatus.Disabled)
        {
            return Result.Failure<SessionPrincipal>(AuthErrors.SessionInvalid);
        }

        // FR-02 / NFR-03: permission checks happen on the server for every
        // privileged action, never in the browser.
        if (!string.IsNullOrWhiteSpace(query.RequiredPermission) &&
            !user.HasPermission(query.RequiredPermission))
        {
            return Result.Failure<SessionPrincipal>(AuthErrors.PermissionDenied);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new SessionPrincipal(
            user.Id,
            user.FullName,
            user.Email.Value,
            [.. user.Roles.Select(r => r.Code)],
            [.. user.Roles.SelectMany(r => r.Permissions).Select(p => p.Code).Distinct().OrderBy(c => c)],
            tokenResult.Value.Hash));
    }
}
