using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartCutScheduler.Application.Common.Interfaces;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Infrastructure.Auth;
using SmartCutScheduler.Infrastructure.Persistence;
using SmartCutScheduler.Infrastructure.Repositories;
using SmartCutScheduler.Infrastructure.Security;

namespace SmartCutScheduler.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Host=localhost;Port=5432;Database=smartcutscheduler;Username=postgres;Password=postgres";

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBarberRepository, BarberRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // JWT
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions
        {
            Issuer = "SmartCutScheduler",
            Audience = "SmartCutSchedulerClient",
            SigningKey = configuration["Jwt:SigningKey"] ?? "SuperSecretKeyForDevelopment12345678901234567890"
        };
        services.AddSingleton(jwtOptions);
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // Password Hashing
        services.AddSingleton<PasswordHasher<User>>();
        services.AddSingleton<IPasswordService, PasswordService>();

        return services;
    }
}
