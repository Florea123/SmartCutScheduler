using System.Reflection;
using System.Text;
using SmartCutScheduler.Api.Data;
using SmartCutScheduler.Api.Features.Auth;
using SmartCutScheduler.Api.Features.Barbers;
using SmartCutScheduler.Api.Features.Services;
using SmartCutScheduler.Api.Features.Availability;
using SmartCutScheduler.Api.Features.Appointments;
using SmartCutScheduler.Api.Infrastructure.Auth;
using SmartCutScheduler.Api.Infrastructure.Security;
using SmartCutScheduler.Api.Infrastructure.Validation;
using SmartCutScheduler.Api.Validation;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// JWT Configuration
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOpts = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions
{
    Issuer = "SmartCutScheduler",
    Audience = "SmartCutSchedulerClient",
    SigningKey = builder.Configuration["Jwt:SigningKey"] ?? "SuperSecretKeyForDevelopment12345678901234567890",
};
builder.Services.AddSingleton(jwtOpts);
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<PasswordHasher<SmartCutScheduler.Api.Domain.User>>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();

// Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOpts.Issuer,
            ValidAudience = jwtOpts.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// OpenAPI (native .NET 10 support)
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// MediatR & FluentValidation
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Host=localhost;Port=5432;Database=smartcutscheduler;Username=postgres;Password=postgres";

// Log connection string for debugging (hide password)
var maskedConnString = connectionString.Contains("Password=") 
    ? System.Text.RegularExpressions.Regex.Replace(connectionString, @"Password=[^;]+", "Password=***")
    : connectionString;
Console.WriteLine($"🔍 DEBUG: Using connection string: {maskedConnString}");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddHttpContextAccessor();

// CORS Configuration
const string corsPolicy = "frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5000",
                "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Middleware
app.UseCors(corsPolicy);
app.UseMiddleware<ValidationExceptionMiddleware>();
app.UseHttpsRedirection();

// Database Migration & Seeding
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<SmartCutScheduler.Api.Domain.User>>();
    
    // TEMPORARY: Skip migrations due to Npgsql 10.0.0 authentication bug
    // await db.Database.MigrateAsync();
    
    var adminName = builder.Configuration["SeedAdmin:Name"] ?? "Admin";
    var adminEmail = builder.Configuration["SeedAdmin:Email"] ?? "admin@smartcut.com";
    var adminPassword = builder.Configuration["SeedAdmin:Password"] ?? "Admin123!";
    
    // Auto-seeding enabled for Docker environment
    await db.EnsureSeedAdminAsync(adminName, adminEmail, adminPassword, hasher);
    await SmartCutScheduler.Api.Infrastructure.Seeding.DatabaseSeeder.SeedDemoDataAsync(db);
}

// OpenAPI + Scalar UI (modern Swagger alternative)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "SmartCutScheduler API";
        options.Theme = ScalarTheme.Purple;
    });
}

app.UseAuthentication();
app.UseAuthorization();

// Map Endpoints
app.MapAuthEndpoints();
app.MapBarberEndpoints();
app.MapServiceEndpoints();
app.MapAvailabilityEndpoints();
app.MapAppointmentEndpoints();

app.Run();

public partial class Program { } // For testing
