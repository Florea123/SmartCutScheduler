using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SmartCutScheduler.Application.Features.Users.GetUserProfile;
using SmartCutScheduler.Application.Common.Models;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.Users;

public class GetUserProfileQueryHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldReturnNull_WhenUserNotFound()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new GetUserProfileQueryHandler(unitOfWorkMock.Object);
        var result = await handler.Handle(new GetUserProfileQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnUserDto_WhenUserFound()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "John",
            Email = "john@test.com",
            PhoneNumber = "123456",
            ProfilePictureUrl = "pic.jpg",
            FreshHaircutPhotoUrl = "fresh.jpg",
            Role = UserRole.Customer,
            CreatedAtUtc = DateTime.UtcNow
        };

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new GetUserProfileQueryHandler(unitOfWorkMock.Object);
        var result = await handler.Handle(new GetUserProfileQuery(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("John");
        result.Email.Should().Be("john@test.com");
        result.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task Handle_ShouldMapAllFields_Correctly()
    {
        var userId = Guid.NewGuid();
        var created = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var user = new User
        {
            Id = userId,
            Name = "Barber Bob",
            Email = "bob@shop.com",
            PhoneNumber = "0722",
            ProfilePictureUrl = null,
            FreshHaircutPhotoUrl = null,
            Role = UserRole.Barber,
            CreatedAtUtc = created
        };

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new GetUserProfileQueryHandler(unitOfWorkMock.Object);
        var result = await handler.Handle(new GetUserProfileQuery(userId), CancellationToken.None);

        result!.Id.Should().Be(userId);
        result.PhoneNumber.Should().Be("0722");
        result.Role.Should().Be("Barber");
        result.CreatedAtUtc.Should().Be(created);
    }
}
