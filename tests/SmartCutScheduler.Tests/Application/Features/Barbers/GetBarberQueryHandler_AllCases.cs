using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SmartCutScheduler.Application.Features.Barbers.GetBarber;
using SmartCutScheduler.Application.Common.Models;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.Barbers;

public class GetBarberQueryHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBarberIsNull()
    {
        var repoMock = new Mock<IBarberRepository>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Barber?)null);

        var handler = new GetBarberQueryHandler(repoMock.Object);
        var result = await handler.Handle(new GetBarberQuery(Guid.NewGuid()), CancellationToken.None);

        result.ToString().Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenBarberExists()
    {
        var barber = new Barber
        {
            Id = Guid.NewGuid(),
            Name = "Alex",
            Description = "Great barber",
            PhotoUrl = "photo.jpg",
            PhoneNumber = "0721",
            Email = "alex@barbershop.com",
            IsActive = true,
            BarberServices = new List<BarberService>(),
            WorkSchedules = new List<WorkSchedule>(),
            Reviews = new List<Review>()
        };

        var repoMock = new Mock<IBarberRepository>();
        repoMock.Setup(r => r.GetByIdAsync(barber.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(barber);

        var handler = new GetBarberQueryHandler(repoMock.Object);
        var result = await handler.Handle(new GetBarberQuery(barber.Id), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
    }

    [Fact]
    public async Task Handle_ShouldCalculateAverageRating_WhenReviewsExist()
    {
        var barber = new Barber
        {
            Id = Guid.NewGuid(),
            Name = "Bob",
            BarberServices = new List<BarberService>(),
            WorkSchedules = new List<WorkSchedule>(),
            Reviews = new List<Review>
            {
                new Review { Rating = 4 },
                new Review { Rating = 5 },
                new Review { Rating = 3 }
            }
        };

        var repoMock = new Mock<IBarberRepository>();
        repoMock.Setup(r => r.GetByIdAsync(barber.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(barber);

        var handler = new GetBarberQueryHandler(repoMock.Object);
        var result = await handler.Handle(new GetBarberQuery(barber.Id), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
        // Extract value and verify rating
        var valueProp = result.GetType().GetProperty("Value");
        var payload = valueProp?.GetValue(result);
        payload.Should().NotBeNull();
        var ratingProp = payload!.GetType().GetProperty("AverageRating");
        var rating = (double)ratingProp!.GetValue(payload)!;
        rating.Should().BeApproximately(4.0, 0.1);
    }

    [Fact]
    public async Task Handle_ShouldReturnZeroRating_WhenNoReviews()
    {
        var barber = new Barber
        {
            Id = Guid.NewGuid(),
            Name = "Charlie",
            BarberServices = new List<BarberService>(),
            WorkSchedules = new List<WorkSchedule>(),
            Reviews = new List<Review>()
        };

        var repoMock = new Mock<IBarberRepository>();
        repoMock.Setup(r => r.GetByIdAsync(barber.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(barber);

        var handler = new GetBarberQueryHandler(repoMock.Object);
        var result = await handler.Handle(new GetBarberQuery(barber.Id), CancellationToken.None);

        var valueProp = result.GetType().GetProperty("Value");
        var payload = valueProp?.GetValue(result);
        var ratingProp = payload!.GetType().GetProperty("AverageRating");
        var rating = (double)ratingProp!.GetValue(payload)!;
        rating.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldIncludeServices_InResult()
    {
        var serviceId = Guid.NewGuid();
        var service = new Service { Id = serviceId, Name = "Haircut", DurationMinutes = 30, BasePrice = 50 };
        var barber = new Barber
        {
            Id = Guid.NewGuid(),
            Name = "Dan",
            BarberServices = new List<BarberService>
            {
                new BarberService { ServiceId = serviceId, Service = service, CustomPrice = 60 }
            },
            WorkSchedules = new List<WorkSchedule>(),
            Reviews = new List<Review>()
        };

        var repoMock = new Mock<IBarberRepository>();
        repoMock.Setup(r => r.GetByIdAsync(barber.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(barber);

        var handler = new GetBarberQueryHandler(repoMock.Object);
        var result = await handler.Handle(new GetBarberQuery(barber.Id), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
    }
}
