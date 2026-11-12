namespace kecas.application.auth.ChangePassword;

using FluentValidation;

internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    /// <summary>
    /// NFR-04 / §8.2: minimum 10 characters, letters and digits, and a small
    /// blocklist of the most-guessed passwords.
    /// </summary>
    private static readonly string[] CommonPasswords =
    [
        "password", "password1", "password123", "1234567890", "123456789",
        "qwertyuiop", "qwerty123", "abc123456", "iloveyou1", "admin12345",
        "letmein123", "welcome123", "monkey12345", "football10", "princess1",
        "kecas", "kecas12345", "funaab12345", "funaab2026",
    ];

    public ChangePasswordCommandValidator()
    {
        RuleFor(c => c.CurrentPassword)
            .NotEmpty();

        RuleFor(c => c.NewPassword)
            .NotEmpty()
            .MinimumLength(10)
            .Matches("[A-Za-z]").WithMessage("The new password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("The new password must contain at least one digit.")
            .Must(NotBeCommon)
            .WithMessage("The new password is too common. Choose something harder to guess.");

        RuleFor(c => c.KeepTokenHash)
            .NotEmpty();
    }

    private static bool NotBeCommon(string password) =>
        !CommonPasswords.Contains(password.Trim().ToLowerInvariant());
}
