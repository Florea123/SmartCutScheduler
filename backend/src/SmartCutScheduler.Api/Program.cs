using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartCutScheduler.Api.Endpoints;
using SmartCutScheduler.Api.Middleware;
using SmartCutScheduler.Application;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Infrastructure;
using SmartCutScheduler.Infrastructure.Auth;
using SmartCutScheduler.Infrastructure.Persistence;
using SmartCutScheduler.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

// Add Application & Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT Configuration
var jwtOpts = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions
{
    Issuer = "SmartCutScheduler",
    Audience = "SmartCutSchedulerClient",
    SigningKey = builder.Configuration["Jwt:SigningKey"] ?? "SuperSecretKeyForDevelopment12345678901234567890",
};

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

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartCutScheduler API",
        Version = "v1",
        Description = "API pentru gestionarea programărilor la frizerie - Clean Architecture"
    });
    
    // JWT Bearer Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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
    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();
    
    // Create database schema
    await db.Database.EnsureCreatedAsync();
    
    var adminName = builder.Configuration["SeedAdmin:Name"] ?? "Admin";
    var adminEmail = builder.Configuration["SeedAdmin:Email"] ?? "admin@smartcut.com";
    var adminPassword = builder.Configuration["SeedAdmin:Password"] ?? "Admin123!";
    
    // Seed admin
    if (!await db.Users.AnyAsync(u => u.Email == adminEmail))
    {
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Name = adminName,
            Email = adminEmail,
            Role = UserRole.Admin,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        admin.PasswordHash = hasher.HashPassword(admin, adminPassword);
        await db.Users.AddAsync(admin);
        await db.SaveChangesAsync();
    }
    
    await DatabaseSeeder.SeedDemoDataAsync(db);
}

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartCutScheduler API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "SmartCutScheduler API Documentation";
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
