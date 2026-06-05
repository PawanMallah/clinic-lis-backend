using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _requestCounts = new();

    private const int GeneralLimitPerMinute = 100;
    private const int AuthLimitPerMinute = 5;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var isAuthEndpoint = path.Contains("/auth/login") || path.Contains("/auth/forgot-password");

        var limit = isAuthEndpoint ? AuthLimitPerMinute : GeneralLimitPerMinute;
        var key = $"{ip}:{(isAuthEndpoint ? "auth" : "general")}";

        var now = DateTime.UtcNow;

        _requestCounts.AddOrUpdate(key,
            _ => (1, now),
            (_, existing) =>
            {
                if ((now - existing.WindowStart).TotalMinutes >= 1)
                    return (1, now);
                return (existing.Count + 1, existing.WindowStart);
            });

        if (_requestCounts.TryGetValue(key, out var current) && current.Count > limit)
        {
            _logger.LogWarning("Rate limit exceeded for IP {IP} on {Path} ({Count}/{Limit})", ip, path, current.Count, limit);

            context.Response.StatusCode = 429;
            context.Response.ContentType = "application/json";
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsync(
                """{"error":"Too many requests. Please try again later.","code":"RATE_LIMITED"}""");
            return;
        }

        if (_requestCounts.Count > 10000)
        {
            var cutoff = now.AddMinutes(-2);
            var staleKeys = _requestCounts.Where(kv => kv.Value.WindowStart < cutoff).Select(kv => kv.Key).ToList();
            foreach (var staleKey in staleKeys)
                _requestCounts.TryRemove(staleKey, out _);
        }

        await _next(context);
    }
}
