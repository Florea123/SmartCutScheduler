using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using Moq;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using Xunit;

using Microsoft.EntityFrameworkCore;
using SmartCutScheduler.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace SmartCutScheduler.Tests.Api
{
    public class ReviewEndpointsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ReviewEndpointsIntegrationTests(WebApplicationFactory<Program> factory)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                    {
                        ["Jwt:SigningKey"] = "TestSigningKeyForIntegrationTests1234567890!"
                    });
                });
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
        public async Task AddOrUpdateReview_Unauthorized_WhenNoToken()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            // Populează baza cu un barber demo
            Guid barberId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SmartCutScheduler.Infrastructure.Persistence.AppDbContext>();
                barberId = Guid.NewGuid();
                db.Barbers.Add(new SmartCutScheduler.Domain.Entities.Barber
                {
                    Id = barberId,
                    Name = "Test Barber",
                    Email = "barber@test.com",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
                db.SaveChanges();
            }
            var review = new Review { BarberId = barberId, Rating = 5, Comment = "Test" };
            var response = await client.PostAsJsonAsync("/api/reviews", review);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // Pentru test autenticat, ar trebui generat un JWT valid cu userId in claims
        // Exemplu de test autenticat (pseudocod, adapteaza la implementarea ta JWT)
        // [Fact]
        // public async Task AddOrUpdateReview_Authorized_ReturnsOk()
        // {
        //     var client = _factory.CreateClient();
        //     var token = GenerateJwtToken("test-user-id");
        //     client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //     var review = new Review { BarberId = Guid.NewGuid(), Rating = 5, Comment = "Test" };
        //     var response = await client.PostAsJsonAsync("/api/Review", review);
        //     Assert.True(response.IsSuccessStatusCode);
        // }
    }
}
