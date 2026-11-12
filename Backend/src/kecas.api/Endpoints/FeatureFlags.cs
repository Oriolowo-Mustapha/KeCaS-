namespace kecas.api.Endpoints;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Sprint 4 feature flags (§6.1: disabled modules 404, they do not error).
/// Each optional module (Notices, Booking, Payments, Feedback) is gated by the
/// configuration key FeatureFlags:{Name}; env vars use FeatureFlags__Notices etc.
/// </summary>
internal static class FeatureFlags
{
    /// <summary>
    /// Filter that makes every endpoint in the group behave as if the module
    /// does not exist (404 Module.Disabled) while the flag is off.
    /// </summary>
    public static RouteHandlerBuilder RequireFeature(this RouteHandlerBuilder builder, string feature) =>
        builder.AddEndpointFilter(new RequireFeatureFilter(feature));

    public static RouteGroupBuilder RequireFeature(this RouteGroupBuilder builder, string feature) =>
        builder.AddEndpointFilter(new RequireFeatureFilter(feature));

    private sealed class RequireFeatureFilter(string feature) : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(
            EndpointFilterInvocationContext context,
            EndpointFilterDelegate next)
        {
            var configuration = context.HttpContext
                .RequestServices
                .GetRequiredService<IConfiguration>();

            var enabled = configuration.GetValue($"FeatureFlags:{feature}", false);
            if (!enabled)
            {
                // Disabled modules look absent (§6.1) — no probing surface.
                return Results.Problem(
                    title: "Module disabled",
                    detail: $"The {feature} module is not enabled on this deployment.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "Module.Disabled",
                    });
            }

            return await next(context);
        }
    }
}
