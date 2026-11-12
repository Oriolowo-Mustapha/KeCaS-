namespace kecas.api.Endpoints;

using kecas.application.abstractions.messaging;
using kecas.application.auth.ChangePassword;
using kecas.application.auth.CurrentUser;
using kecas.application.auth.Login;
using kecas.application.auth.Logout;
using MediatR;

/// <summary>
/// FR-01 auth module: /auth/login, /auth/logout, /auth/me, /auth/change-password.
/// The session travels in an httpOnly, SameSite=Lax cookie (§6.1); only its
/// hash is stored in the database.
/// </summary>
public static class AuthEndpoints
{
    public const string CookieName = "kecas_session";

    private static readonly TimeSpan CookieLifetime = TimeSpan.FromHours(24);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/login", async (
            LoginCommand command,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                return VehicleEndpoints.ErrorToProblem(result.Error);
            }

            var user = result.Value;
            http.Response.Cookies.Append(CookieName, user.Token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                // Secure when served over HTTPS (Caddy in deployment); relaxed
                // over plain HTTP so localhost testing with cookies works.
                Secure = http.Request.IsHttps,
                MaxAge = CookieLifetime,
                Path = "/",
                IsEssential = true,
            });

            // §8.2: the CSRF token is per-session — rotate it on login.
            CsrfMiddleware.AppendTokenCookie(
                http, CsrfMiddleware.NewToken());

            return Results.Ok(new
            {
                id = user.Id,
                fullName = user.FullName,
                email = user.Email,
                roles = user.Roles,
                permissions = user.Permissions,
            });
        })
        .AllowAnonymous()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status429TooManyRequests);

        group.MapPost("/logout", async (
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var principal = (SessionPrincipal)http.Items[RequirePermissionFilter.PrincipalKey]!;
            var result = await sender.Send(
                new LogoutCommand(principal.Id, principal.TokenHash), cancellationToken);

            http.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });

            return result.IsSuccess
                ? Results.NoContent()
                : VehicleEndpoints.ErrorToProblem(result.Error);
        })
        .RequireSession()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", (HttpContext http) =>
        {
            var principal = (SessionPrincipal)http.Items[RequirePermissionFilter.PrincipalKey]!;
            return Results.Ok(new
            {
                id = principal.Id,
                fullName = principal.FullName,
                email = principal.Email,
                roles = principal.Roles,
                permissions = principal.Permissions,
            });
        })
        .RequireSession()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/change-password", async (
            ChangePasswordRequest request,
            ISender sender,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var principal = (SessionPrincipal)http.Items[RequirePermissionFilter.PrincipalKey]!;
            var command = new ChangePasswordCommand(
                principal.Id,
                request.CurrentPassword,
                request.NewPassword,
                principal.TokenHash);

            var result = await sender.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                return VehicleEndpoints.ErrorToProblem(result.Error);
            }

            // The current session stays valid; every other session was revoked.
            return Results.NoContent();
        })
        .RequireSession()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        return app;
    }
}

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
