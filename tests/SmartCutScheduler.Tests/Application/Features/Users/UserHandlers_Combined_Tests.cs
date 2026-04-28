using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using SmartCutScheduler.Application.Common.Interfaces;
using SmartCutScheduler.Application.Features.Users.DeleteUser;
using SmartCutScheduler.Application.Features.Users.GetAllUsers;
using SmartCutScheduler.Application.Features.Users.UpdateFreshHaircutPhoto;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.Users;

public class UserHandlers_Combined_Tests
{
    // ─────────────────────────── GetAllUsersQueryHandler ────────────────────────────

    [Fact]
    public async Task GetAllUsers_ShouldReturnOk_WithUserList()
    {
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@x.com", PhoneNumber = "0700" },
            new User { Id = Guid.NewGuid(), Name = "Bob",   Email = "bob@x.com",   PhoneNumber = "0701" }
        };
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var handler = new GetAllUsersQueryHandler(repoMock.Object);
        var result = await handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.ToString().Should().Contain("Ok");
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnOk_WithEmptyList()
    {
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());

        var handler = new GetAllUsersQueryHandler(repoMock.Object);
        var result = await handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
    }

    // ─────────────────────────── DeleteUserCommandHandler ───────────────────────────

    [Fact]
    public async Task DeleteUser_ShouldReturnNotFound_WhenUserMissing()
    {
        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.Users.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null!);

        var handler = new DeleteUserCommandHandler(uowMock.Object);
        var result = await handler.Handle(new DeleteUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.ToString().Should().Contain("NotFound");
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnOk_WhenUserDeleted()
    {
        var user = new User { Id = Guid.NewGuid(), Name = "Alice" };
        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        uowMock.Setup(u => u.Users.DeleteAsync(user.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteUserCommandHandler(uowMock.Object);
        var result = await handler.Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
    }

    // ──────────────────── UpdateFreshHaircutPhotoCommandHandler ─────────────────────

    [Fact]
    public async Task UpdateFreshHaircutPhoto_ShouldThrow_WhenUserNotFound()
    {
        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.Users.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null!);
        var fileServiceMock = new Mock<IFileStorageService>();

        var handler = new UpdateFreshHaircutPhotoCommandHandler(uowMock.Object, fileServiceMock.Object);
        var fileMock = new Mock<IFormFile>();
        var command = new UpdateFreshHaircutPhotoCommand { UserId = Guid.NewGuid(), Photo = fileMock.Object };

        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateFreshHaircutPhoto_ShouldSaveAndReturnUrl()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var fileMock = new Mock<IFormFile>();
        var fileServiceMock = new Mock<IFileStorageService>();
        fileServiceMock.Setup(f => f.SaveFreshHaircutPhotoAsync(userId, fileMock.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync("uploads/fresh.jpg");

        var handler = new UpdateFreshHaircutPhotoCommandHandler(uowMock.Object, fileServiceMock.Object);
        var command = new UpdateFreshHaircutPhotoCommand { UserId = userId, Photo = fileMock.Object };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be("uploads/fresh.jpg");
        user.FreshHaircutPhotoUrl.Should().Be("uploads/fresh.jpg");
    }
}
