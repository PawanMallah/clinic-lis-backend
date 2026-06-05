using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;

namespace Shared.Middleware;

public class JwtClaimsMiddleware
{
    private readonly RequestDelegate _next;

    public JwtClaimsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();

            try
            {
                var handler = new JwtSecurityTokenHandler();

                if (handler.CanReadToken(token))
                {
                    var jwtToken = handler.ReadJwtToken(token);

                    var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                    var role = jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
                    var labId = jwtToken.Claims.FirstOrDefault(c => c.Type == "labId")?.Value;
                    var name = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
                    var email = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;

                    if (!string.IsNullOrEmpty(userId))
                        context.Items["userId"] = userId;
                    if (!string.IsNullOrEmpty(role))
                        context.Items["role"] = role;
                    if (!string.IsNullOrEmpty(labId))
                        context.Items["labId"] = labId;
                    if (!string.IsNullOrEmpty(name))
                        context.Items["name"] = name;
                    if (!string.IsNullOrEmpty(email))
                        context.Items["email"] = email;
                }
            }
            catch
            {
                // Token parsing failed - continue without setting claims
            }
        }

        await _next(context);
    }
}
