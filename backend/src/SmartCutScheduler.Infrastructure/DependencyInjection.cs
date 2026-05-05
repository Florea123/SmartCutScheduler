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

        // Adaugă providerul Npgsql doar dacă environment-ul NU este 'Testing'
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (!string.Equals(env, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBarberRepository, BarberRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
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

        // File Storage
        services.Configure<FileStorage.AzureBlobStorageOptions>(
            configuration.GetSection(FileStorage.AzureBlobStorageOptions.SectionName));
        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileStorage.AzureBlobStorageOptions>>().Value;
            return new Azure.Storage.Blobs.BlobServiceClient(opts.ConnectionString);
        });
        services.AddScoped<IFileStorageService, FileStorage.AzureBlobStorageService>();

        return services;
    }
}
