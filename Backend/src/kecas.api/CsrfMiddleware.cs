namespace kecas.api;

using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using System.Text;

/// <summary>
/// §8.2 / NFR-03: cross-site request forgery protection on top of the
/// SameSite=Lax session cookie — a double-submit token. Every response
/// carries a random "csrf" cookie (readable by the SPA so it can echo it),
/// and every state-changing request under /api must send the same value in
/// the X-CSRF-Token header. Login is exempt (there is no session yet) and
/// rotates the token on success so each session gets its own value.
/// Failure is 403 problem+json with code Csrf.Invalid (§6.1).
/// </summary>
public sealed class CsrfMiddleware(RequestDelegate next)
{
    public const string CookieName = "csrf";
    public const string HeaderName = "X-CSRF-Token";

    private static readonly string[] UnsafeMethods = ["POST", "PUT", "PATCH", "DELETE"];

    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public async Task InvokeAsync(HttpContext context)
    {
        // Issue a fresh token when the client does not have one yet, so the
        // very next request can already send the header.
        var cookieToken = context.Request.Cookies[CookieName];
        if (string.IsNullOrEmpty(cookieToken))
        {
            AppendTokenCookie(context, NewToken());
            cookieToken = null;
        }

        var requiresToken = UnsafeMethods.Contains(
            context.Request.Method,
            StringComparer.OrdinalIgnoreCase)
            && context.Request.Path.StartsWithSegments("/api")
            && !context.Request.Path.StartsWithSegments("/api/v1/auth/login");

        if (requiresToken)
        {
            var headerToken = context.Request.Headers[HeaderName].FirstOrDefault();
            if (cookieToken is null || headerToken is null
                || !FixedTimeEquals(headerToken, cookieToken))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.RequestServices
                    .GetRequiredService<IProblemDetailsService>()
                    .TryWriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = context,
                        ProblemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status403Forbidden,
                            Title = "CSRF token missing or invalid.",
                            Extensions = new Dictionary<string, object?>
                            {
                                ["code"] = "Csrf.Invalid",
                            },
                        },
                    });
                return;
            }
        }

        await next(context);
    }

    /// <summary>A fresh random token for a Set-Cookie header value.</summary>
    public static string NewToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>Rotates the token on login so it is bound to the new session.</summary>
    public static void AppendTokenCookie(HttpContext context, string token)
    {
        context.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = false, // the SPA reads the cookie to send the header
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            MaxAge = Lifetime,
            Path = "/",
            IsEssential = true,
        });
    }

    /// <summary>Length-safe comparison that does not short-circuit on the first difference.</summary>
    private static bool FixedTimeEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var difference = 0;
        for (var index = 0; index < left.Length; index++)
        {
            difference |= left[index] ^ right[index];
        }

        return difference == 0;
    }
}
