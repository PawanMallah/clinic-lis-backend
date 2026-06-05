using LIS.ReportService.Repositories;
using Shared.Cache;
using Shared.Extensions;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Shared services
builder.Services.AddDapperContext(builder.Configuration);
builder.Services.AddJwtHelper(builder.Configuration);
builder.Services.AddRedisCache(builder.Configuration);

// Register repositories
builder.Services.AddScoped<IReportRepository, ReportRepository>();

// Add controllers
builder.Services.AddControllers();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Middleware pipeline
app.UseCors();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
// app.UseMiddleware<JwtClaimsMiddleware>(); // Disabled for dev (no auth)
app.MapControllers();

app.Run();
