using Microsoft.AspNetCore.Http;

namespace Shared.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self' ws: wss:;";

        if (!context.Request.Host.Host.Contains("localhost"))
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (path.Contains("/results") || path.Contains("/reports") || path.Contains("/specimens"))
        {
            context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
            context.Response.Headers["Pragma"] = "no-cache";
        }

        await _next(context);
    }
}
