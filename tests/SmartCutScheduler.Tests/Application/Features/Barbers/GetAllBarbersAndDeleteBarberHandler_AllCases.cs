using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SmartCutScheduler.Application.Features.Barbers.DeleteBarber;
using SmartCutScheduler.Application.Features.Barbers.GetAllBarbers;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.Barbers;

public class GetAllBarbersQueryHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldReturnOk_WithEmptyList_WhenNoBarbersExist()
    {
        var repoMock = new Mock<IBarberRepository>();
        repoMock.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Barber>());
        var handler = new GetAllBarbersQueryHandler(repoMock.Object);

        var result = await handler.Handle(new GetAllBarbersQuery(), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WithMappedDtoList()
    {
        var barberId = Guid.NewGuid();
        var barber = new Barber
        {
            Id = barberId,
            Name = "Dan",
            Description = "Expert",
            IsActive = true,
            BarberServices = new List<BarberService>(),
            Reviews = new List<Review>()
        };
        var repoMock = new Mock<IBarberRepository>();
        repoMock.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Barber> { barber });
        var handler = new GetAllBarbersQueryHandler(repoMock.Object);

        var result = await handler.Handle(new GetAllBarbersQuery(), CancellationToken.None);

        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<
            System.Collections.Generic.List<SmartCutScheduler.Application.Common.Models.BarberDto>>;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().HaveCount(1);
        okResult.Value![0].Name.Should().Be("Dan");
        okResult.Value[0].AverageRating.Should().Be(0);
        okResult.Value[0].ReviewCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldComputeAverageRating_WhenBarberHasReviews()
    {
        var barberId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var barber = new Barber
        {
            Id = barberId,
            Name = "Alex",
            IsActive = true,
            BarberServices = new List<BarberService>(),
            Reviews = new List<Review>
            {
                new Review { Id = Guid.NewGuid(), BarberId = barberId, UserId = userId, Rating = 4 },
                new Review { Id = Guid.NewGuid(), BarberId = barberId, UserId = Guid.NewGuid(), Rating = 5 }
            }
        };
        var repoMock = new Mock<IBarberRepository>();
        repoMock.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Barber> { barber });
        var handler = new GetAllBarbersQueryHandler(repoMock.Object);

        var result = await handler.Handle(new GetAllBarbersQuery(), CancellationToken.None);

        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<
            System.Collections.Generic.List<SmartCutScheduler.Application.Common.Models.BarberDto>>;
        okResult.Should().NotBeNull();
        okResult!.Value![0].AverageRating.Should().Be(4.5);
        okResult.Value[0].ReviewCount.Should().Be(2);
    }
}

public class DeleteBarberCommandHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBarberDoesNotExist()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Barber?)null);
        var handler = new DeleteBarberCommandHandler(unitOfWorkMock.Object);

        var result = await handler.Handle(new DeleteBarberCommand(Guid.NewGuid()), CancellationToken.None);

        result.ToString().Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenBarberExists()
    {
        var barberId = Guid.NewGuid();
        var barber = new Barber { Id = barberId, Name = "Test" };
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Barbers.GetByIdAsync(barberId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(barber);
        unitOfWorkMock.Setup(u => u.Barbers.DeleteAsync(barberId, It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(1);
        var handler = new DeleteBarberCommandHandler(unitOfWorkMock.Object);

        var result = await handler.Handle(new DeleteBarberCommand(barberId), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
        unitOfWorkMock.Verify(u => u.Barbers.DeleteAsync(barberId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
