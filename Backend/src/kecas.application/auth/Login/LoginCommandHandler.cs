namespace kecas.application.auth.Login;

using kecas.application.abstractions.authentication;
using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;

internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;

        // One message for unknown user, wrong password and a disabled account,
        // so responses never reveal whether an account exists (§8.2).
        var emailResult = Email.Create(command.Email);
        var user = emailResult.IsSuccess
            ? await userRepository.GetByEmailAsync(emailResult.Value.Value, cancellationToken)
            : null;

        if (user is null)
        {
            return Result.Failure<LoginResponse>(AuthErrors.InvalidCredentials);
        }

        // 5 failed attempts in 15 minutes lock the account for 15 minutes.
        if (user.IsLockedOut(now))
        {
            return Result.Failure<LoginResponse>(AuthErrors.AccountLocked);
        }

        if (!passwordHasher.Verify(user.PasswordHash, command.Password))
        {
            user.RecordFailedLogin(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<LoginResponse>(AuthErrors.InvalidCredentials);
        }

        var loginResult = user.RecordSuccessfulLogin(now);
        if (loginResult.IsFailure)
        {
            // Disabled accounts cannot sign in, but the response stays generic.
            return Result.Failure<LoginResponse>(AuthErrors.InvalidCredentials);
        }

        var token = SessionToken.New();
        sessionRepository.Add(Session.Create(user.Id, token, now));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResponse(
            user.Id,
            user.FullName,
            user.Email.Value,
            [.. user.Roles.Select(r => r.Code)],
            [.. user.Roles.SelectMany(r => r.Permissions).Select(p => p.Code).Distinct().OrderBy(c => c)],
            token.Raw));
    }
}
