using Dapper;
using Microsoft.AspNetCore.Http;
using Shared.Database;
using System.Security.Cryptography;
using System.Text;

namespace Shared.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, DapperContext dbContext)
    {
        // Only apply to /api/v1 routes
        if (!context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await _next(context);
            return;
        }

        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"success\":false,\"message\":\"API key is required. Provide X-API-Key header.\"}");
            return;
        }

        var keyHash = HashApiKey(apiKey);

        using var connection = dbContext.CreateConnection();
        var keyRecord = await connection.QueryFirstOrDefaultAsync<ApiKeyRecord>(
            @"SELECT id, lab_id AS LabId, key_name AS KeyName, permissions, is_active AS IsActive, 
                     rate_limit_per_minute AS RateLimitPerMinute, expires_at AS ExpiresAt
              FROM api_keys 
              WHERE api_key_hash = @Hash",
            new { Hash = keyHash });

        if (keyRecord == null)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"success\":false,\"message\":\"Invalid API key.\"}");
            return;
        }

        if (!keyRecord.IsActive)
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"success\":false,\"message\":\"API key is inactive.\"}");
            return;
        }

        if (keyRecord.ExpiresAt.HasValue && keyRecord.ExpiresAt.Value < DateTime.UtcNow)
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"success\":false,\"message\":\"API key has expired.\"}");
            return;
        }

        // Set context items for downstream use
        context.Items["labId"] = keyRecord.LabId.ToString();
        context.Items["apiKeyId"] = keyRecord.Id.ToString();
        context.Items["apiKeyName"] = keyRecord.KeyName;
        context.Items["apiKeyPermissions"] = keyRecord.Permissions;

        // Update last_used_at asynchronously
        _ = connection.ExecuteAsync(
            "UPDATE api_keys SET last_used_at = NOW() WHERE id = @Id",
            new { Id = keyRecord.Id });

        await _next(context);
    }

    private static string HashApiKey(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private class ApiKeyRecord
    {
        public Guid Id { get; set; }
        public Guid LabId { get; set; }
        public string KeyName { get; set; } = string.Empty;
        public string? Permissions { get; set; }
        public bool IsActive { get; set; }
        public int RateLimitPerMinute { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
