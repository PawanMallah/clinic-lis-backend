using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Cache;
using Shared.Database;
using Shared.Helpers;

namespace Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDapperContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        services.AddSingleton(new DapperContext(connectionString));
        return services;
    }

    public static IServiceCollection AddJwtHelper(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = new JwtSettings();
        configuration.GetSection("Jwt").Bind(jwtSettings);
        services.AddSingleton(jwtSettings);
        services.AddSingleton(new JwtHelper(jwtSettings));
        return services;
    }

    public static IServiceCollection AddLisCache(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddRedisCache(configuration);
    }
}
