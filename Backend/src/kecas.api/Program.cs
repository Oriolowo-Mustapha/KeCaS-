using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using kecas.api;
using kecas.api.Endpoints;
using kecas.application;
using kecas.infrastructure;
using kecas.infrastructure.Configuration;
using kecas.infrastructure.Seeding;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;

// All environment variables load from the repo-root .env file.
// Real environment variables take precedence over .env values.
DotEnv.Load();

// `dotnet run --project src/kecas.api -- seed`: apply migrations and insert
// the seed data (Appendix A + §8.1), then exit. Never starts the web server.
if (args.Contains("seed"))
{
    await SeedRunner.RunAsync();
    return;
}

// NFR-05: rewrite legacy plaintext licence numbers as AES-256-GCM ciphertext.
if (args.Contains("encrypt-licences"))
{
    await LicenceEncryptionRunner.RunAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);

// §11.4: structured logs with request IDs; no sensitive values in logs.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Enums travel as SCREAMING_SNAKE words matching the design doc storage
// values: KEKE, CAB, SHUTTLE, IN_PROGRESS, PENDING_APPROVAL, AUTO, ...
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
});

// Swagger UI for exploring and testing the endpoints while the team builds.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "KeCaS API",
        Version = "v1",
        Description = "Keke, Cab and Shuttle Transportation Management System (FUNAAB). "
            + "Base path /api/v1 (design doc §6.1).",
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// §8.2: plate verification is rate limited to 20 requests per hour per IP
// so nobody can scrape the vehicle registry (DD-12). Rejections are 429.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("verify-vehicle", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
            }));
    options.OnRejected = async (rejectContext, cancellationToken) =>
    {
        rejectContext.HttpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;
        await rejectContext.HttpContext.RequestServices
            .GetRequiredService<IProblemDetailsService>()
            .TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = rejectContext.HttpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests. Try again later.",
                    Extensions = new Dictionary<string, object?>
                    {
                        ["code"] = "RateLimiter.Limited",
                    },
                },
            });
    };
});

// DD-10: public GET responses are cached 60 seconds so cheap phones on weak
// connections get cheap, fast reads while staff data stays live.
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("public60", policy => policy
        .Expire(TimeSpan.FromSeconds(60))
        .SetVaryByRouteValue("*")
        .SetVaryByQuery("*"));
});

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "KeCaS API v1");
        options.DocumentTitle = "KeCaS API";
    });
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

// §8.2 / NFR-03: CSRF double-submit token on state-changing requests.
app.UseMiddleware<CsrfMiddleware>();

// DD-12: the 20-per-hour-per-IP plate-check limit lives in the limiter above.
app.UseRateLimiter();

// The cache policy only touches the anonymous /public endpoints.
app.UseOutputCache();

// §11.4: /healthz checks the API and the database.
app.MapHealthChecks("/healthz");

app.MapVehicleEndpoints();

app.MapAuthEndpoints();

app.MapUserEndpoints();

app.MapDriverEndpoints();

app.MapAuditEndpoints();

app.MapNetworkEndpoints();

app.MapFareEndpoints();

app.MapShiftEndpoints();

app.MapTripEndpoints();

app.MapNoticeEndpoints();

app.MapBookingEndpoints();

app.MapPaymentEndpoints();
app.MapReportEndpoints();

app.MapFeedbackEndpoints();

app.MapPublicEndpoints();

app.Run();
