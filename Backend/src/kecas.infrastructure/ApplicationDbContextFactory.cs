namespace kecas.infrastructure;

using kecas.infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Lets `dotnet ef` build migrations without a running API or a live database.
/// Loads the same .env file as the API, then reads the connection string from
/// the environment (KECAS_DATABASE overrides ConnectionStrings__Database),
/// falling back to the local docker-compose default.
/// </summary>
internal sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        DotEnv.Load();

        var connectionString = Environment.GetEnvironmentVariable("KECAS_DATABASE")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Database")
            ?? "Host=localhost;Port=5432;Database=kecas;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

        return new ApplicationDbContext(optionsBuilder.Options, mediator: null!);
    }
}
