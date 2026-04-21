using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SmartCutScheduler.Api.Endpoints;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

using Microsoft.EntityFrameworkCore;
using SmartCutScheduler.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace SmartCutScheduler.Tests.Api
{
    public class ProgramIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ProgramIntegrationTests(WebApplicationFactory<Program> factory)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove ALL registrations for AppDbContext and its options (compatibil universal)
                    for (int i = services.Count - 1; i >= 0; i--)
                    {
                        var s = services[i];
                        if (s.ServiceType == typeof(DbContextOptions<AppDbContext>) || s.ServiceType == typeof(AppDbContext))
                        {
                            services.RemoveAt(i);
                        }
                    }

                    // Add InMemory provider
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });
                });
            });
        }

        [Fact]
        public async Task Get_BarbersEndpoint_ReturnsSuccess()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var response = await client.GetAsync("/api/barbers");
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Get_ReviewsForBarber_ReturnsOk()
        {
            var client = _factory.CreateClient();
            // Populează baza cu un barber demo
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SmartCutScheduler.Infrastructure.Persistence.AppDbContext>();
                var barberId = Guid.NewGuid();
                db.Barbers.Add(new SmartCutScheduler.Domain.Entities.Barber
                {
                    Id = barberId,
                    Name = "Test Barber",
                    Email = "barber@test.com",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
                db.SaveChanges();

                var response = await client.GetAsync($"/api/reviews/barber/{barberId}");
                Assert.True(response.IsSuccessStatusCode);
            }
        }
    }
}
