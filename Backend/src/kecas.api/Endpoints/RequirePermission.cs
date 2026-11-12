namespace kecas.api.Endpoints;

using kecas.application.abstractions.messaging;
using kecas.application.auth.CurrentUser;
using MediatR;

/// <summary>
/// FR-02 / NFR-03: server-side permission enforcement. The filter validates
/// the session cookie and the required permission before the endpoint handler
/// ever runs, and stores the user's identity in HttpContext.Items for
/// handlers that need it (logout, change-password).
/// </summary>
internal sealed class RequirePermissionFilter(string? requiredPermission) : IEndpointFilter
{
    public const string PrincipalKey = "kecas.user";

    /// <summary>The identity validated by the filter, for handlers that need it.</summary>
    public static SessionPrincipal Principal(HttpContext http) =>
        (SessionPrincipal)http.Items[PrincipalKey]!;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var token = http.Request.Cookies[AuthEndpoints.CookieName];

        var sender = http.RequestServices.GetRequiredService<ISender>();
        var result = await sender.Send(
            new ValidateSessionQuery(token, requiredPermission),
            http.RequestAborted);

        if (result.IsFailure)
        {
            // 401 (not signed in / session expired) or 403 (no permission),
            // both in the §6.1 error envelope.
            return VehicleEndpoints.ErrorToProblem(result.Error);
        }

        http.Items[PrincipalKey] = result.Value;

        return await next(context);
    }
}

public static class RequirePermissionExtensions
{
    /// <summary>Only signed-in users holding the permission may proceed.</summary>
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission) =>
        builder.AddEndpointFilter(new RequirePermissionFilter(permission));

    /// <summary>Any signed-in user may proceed.</summary>
    public static RouteHandlerBuilder RequireSession(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter(new RequirePermissionFilter(null));
}
