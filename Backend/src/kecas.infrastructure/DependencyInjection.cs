namespace kecas.infrastructure;

using kecas.application.abstractions.authentication;
using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;
using kecas.domain.abstractions;
using kecas.domain.fares;
using kecas.domain.feedback;
using kecas.domain.fleet;
using kecas.domain.bookings;
using kecas.domain.network;
using kecas.domain.notices;
using kecas.domain.payments;
using kecas.domain.trips;
using kecas.domain.users;
using kecas.infrastructure.Authentication;
using kecas.infrastructure.BackgroundServices;
using kecas.infrastructure.Clock;
using kecas.infrastructure.Configuration;
using kecas.infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The database connection string is missing. Set ConnectionStrings__Database "
                + "in the .env file (or a real environment variable / user-secrets).");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IOperatorRepository, OperatorRepository>();
        services.AddScoped<IExistenceChecker, ExistenceChecker>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IAuditReader, AuditReader>();
        services.AddScoped<IZoneRepository, ZoneRepository>();
        services.AddScoped<IStopRepository, StopRepository>();
        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<IFareRepository, FareRepository>();
        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<INoticeRepository, NoticeRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IReportQueries, ReportQueries>();

        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // The stale-shift guard (§5.1): closes shifts left open too long.
        services.AddHostedService<StaleShiftSweeper>();

        // The notice expiry job (FR-22, §3.1): expires notices past their end.
        services.AddHostedService<NoticeExpirySweeper>();

        services.AddHealthChecks()
            .AddNpgSql(connectionString);

        return services;
    }
}
