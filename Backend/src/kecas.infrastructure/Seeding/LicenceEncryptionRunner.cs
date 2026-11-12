namespace kecas.infrastructure.Seeding;

using kecas.application;
using kecas.infrastructure.Configuration;
using kecas.infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// One-time hardening command: rewrites every driver licence number still
/// stored as plaintext into AES-256-GCM ciphertext (NFR-05).
/// Run with:  dotnet run --project src/kecas.api -- encrypt-licences
/// Idempotent: rows already holding an "enc1:..." value are skipped, and the
/// live API keeps reading legacy plaintext until this command has run.
/// </summary>
public static class LicenceEncryptionRunner
{
    public static async Task RunAsync()
    {
        Console.WriteLine("Licence encryption starting...");

        DotEnv.Load();

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        await using var provider = services.BuildServiceProvider();

        var context = provider.GetRequiredService<ApplicationDbContext>();

        // The licence protector is wired into the DriverConfiguration value
        // converter, so reads of legacy plaintext work unchanged; here the
        // rows are rewritten with raw SQL because the model now transparently
        // encrypts on write and would otherwise leave them untouched.
        var protector = new AesGcmLicenceProtector();

        // SqlQuery<T> maps result columns to the CLR property names using the
        // project's snake_case convention (id, licence_number) — which the
        // drivers table already uses, so no aliases are needed.
        var plaintextRows = await context.Database
            .SqlQuery<LicenceRow>($"SELECT id, licence_number FROM drivers WHERE licence_number NOT LIKE 'enc1:%'")
            .ToListAsync();

        foreach (var row in plaintextRows)
        {
            var encrypted = protector.Encrypt(row.LicenceNumber);
            await context.Database
                .ExecuteSqlAsync($"UPDATE drivers SET licence_number = {encrypted} WHERE id = {row.Id}");
        }

        Console.WriteLine(
            $"Licence encryption done: {plaintextRows.Count} rows rewritten to AES-256-GCM.");
    }

    private sealed record LicenceRow(Guid Id, string LicenceNumber);
}
