using SmartCutScheduler.Api.Data;
using SmartCutScheduler.Api.Domain;
using SmartCutScheduler.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace SmartCutScheduler.Api.Infrastructure.Seeding;

public static class DatabaseSeeder
{
    public static async Task SeedDemoDataAsync(AppDbContext db)
    {
        // Skip if data already exists
        if (await db.Barbers.AnyAsync() || await db.Services.AnyAsync())
            return;

        // Seed Services
        var services = new List<Service>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Tuns Clasic",
                Description = "Tuns clasic pentru bărbați",
                DurationMinutes = 30,
                BasePrice = 50m,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Tuns + Barbă",
                Description = "Tuns și aranjare barbă",
                DurationMinutes = 45,
                BasePrice = 80m,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Tuns Copii",
                Description = "Tuns pentru copii până în 12 ani",
                DurationMinutes = 20,
                BasePrice = 35m,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Aranjare Barbă",
                Description = "Aranjare și conturare barbă",
                DurationMinutes = 20,
                BasePrice = 30m,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Tuns Premium",
                Description = "Tuns premium cu styling și finishing",
                DurationMinutes = 60,
                BasePrice = 120m,
                IsActive = true
            }
        };

        await db.Services.AddRangeAsync(services);
        await db.SaveChangesAsync();

        // Seed Barbers
        var barbers = new List<Barber>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Alexandru Popescu",
                Description = "15 ani experiență. Specializat în tunsori clasice și moderne.",
                PhoneNumber = "+40721123456",
                Email = "alex.popescu@smartcut.com",
                PhotoUrl = null,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Mihai Ionescu",
                Description = "Expert în hair styling și fade cuts. Campion național barber.",
                PhoneNumber = "+40721234567",
                Email = "mihai.ionescu@smartcut.com",
                PhotoUrl = null,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Dan Cristea",
                Description = "Specialist în aranjare barbă și tunsori moderne.",
                PhoneNumber = "+40721345678",
                Email = "dan.cristea@smartcut.com",
                PhotoUrl = null,
                IsActive = true
            }
        };

        await db.Barbers.AddRangeAsync(barbers);
        await db.SaveChangesAsync();

        // Seed BarberServices (associate barbers with services)
        var barberServices = new List<BarberService>();

        // Alexandru offers all services
        foreach (var service in services)
        {
            barberServices.Add(new BarberService
            {
                BarberId = barbers[0].Id,
                ServiceId = service.Id,
                CustomPrice = null // Uses base price
            });
        }

        // Mihai offers all except children's haircut
        foreach (var service in services.Where(s => s.Name != "Tuns Copii"))
        {
            barberServices.Add(new BarberService
            {
                BarberId = barbers[1].Id,
                ServiceId = service.Id,
                CustomPrice = service.Name == "Tuns Premium" ? 150m : null // Premium price for Mihai
            });
        }

        // Dan offers basic services
        foreach (var service in services.Where(s => s.Name is "Tuns Clasic" or "Aranjare Barbă" or "Tuns + Barbă"))
        {
            barberServices.Add(new BarberService
            {
                BarberId = barbers[2].Id,
                ServiceId = service.Id,
                CustomPrice = null
            });
        }

        await db.BarberServices.AddRangeAsync(barberServices);
        await db.SaveChangesAsync();

        // Seed Work Schedules (all barbers work Mon-Sat, 8:00-18:00)
        var workSchedules = new List<WorkSchedule>();

        foreach (var barber in barbers)
        {
            for (int day = 1; day <= 6; day++) // Monday to Saturday
            {
                workSchedules.Add(new WorkSchedule
                {
                    Id = Guid.NewGuid(),
                    BarberId = barber.Id,
                    DayOfWeek = (DayOfWeekEnum)day,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0),
                    IsWorkingDay = true
                });
            }

            // Sunday - not working
            workSchedules.Add(new WorkSchedule
            {
                Id = Guid.NewGuid(),
                BarberId = barber.Id,
                DayOfWeek = DayOfWeekEnum.Sunday,
                StartTime = new TimeSpan(0, 0, 0),
                EndTime = new TimeSpan(0, 0, 0),
                IsWorkingDay = false
            });
        }

        await db.WorkSchedules.AddRangeAsync(workSchedules);
        await db.SaveChangesAsync();

        Console.WriteLine("✅ Demo data seeded successfully!");
        Console.WriteLine($"   - {services.Count} services");
        Console.WriteLine($"   - {barbers.Count} barbers");
        Console.WriteLine($"   - {barberServices.Count} barber-service associations");
        Console.WriteLine($"   - {workSchedules.Count} work schedules");
    }
}
