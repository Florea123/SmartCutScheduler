using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using SmartCutScheduler.Application.Features.Appointments.CreateAppointment;
using SmartCutScheduler.Application.Features.Auth.Login;
using SmartCutScheduler.Application.Features.BarberServices.GetServices;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features;

// ════════════════════════════════ LoginCommandValidator ════════════════════════════════

public class LoginCommandValidator_Tests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenCredentialsValid()
    {
        var result = _validator.TestValidate(new LoginCommand("test@example.com", "Password123"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_WhenEmailEmpty()
    {
        var result = _validator.TestValidate(new LoginCommand("", "Password"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Fail_WhenEmailInvalidFormat()
    {
        var result = _validator.TestValidate(new LoginCommand("not-an-email", "Password"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Fail_WhenPasswordEmpty()
    {
        var result = _validator.TestValidate(new LoginCommand("test@example.com", ""));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}

// ════════════════════════════ CreateAppointmentCommandValidator ════════════════════════

public class CreateAppointmentCommandValidator_Tests
{
    private readonly CreateAppointmentCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var command = new CreateAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today.AddDays(1), "10:00", null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_WhenBarberIdEmpty()
    {
        var command = new CreateAppointmentCommand(Guid.Empty, Guid.NewGuid(), DateTime.Today.AddDays(1), "10:00", null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BarberId);
    }

    [Fact]
    public void Should_Fail_WhenServiceIdEmpty()
    {
        var command = new CreateAppointmentCommand(Guid.NewGuid(), Guid.Empty, DateTime.Today.AddDays(1), "10:00", null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ServiceId);
    }

    [Fact]
    public void Should_Fail_WhenDateInPast()
    {
        var command = new CreateAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today.AddDays(-1), "10:00", null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AppointmentDate);
    }

    [Fact]
    public void Should_Fail_WhenStartTimeInvalidFormat()
    {
        var command = new CreateAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today.AddDays(1), "25:00", null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.StartTime);
    }
}

// ════════════════════════════════ GetServicesQueryHandler ═════════════════════════════

public class GetServicesQueryHandler_Tests
{
    [Fact]
    public async Task Handle_ShouldReturnOk_WithServiceList()
    {
        var services = new List<Service>
        {
            new Service { Id = Guid.NewGuid(), Name = "Haircut", DurationMinutes = 30, BasePrice = 50, IsActive = true },
            new Service { Id = Guid.NewGuid(), Name = "Shave",   DurationMinutes = 20, BasePrice = 30, IsActive = true }
        };
        var repoMock = new Mock<IServiceRepository>();
        repoMock.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync(services);

        var handler = new GetServicesQueryHandler(repoMock.Object);
        var result = await handler.Handle(new GetServicesQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.ToString().Should().Contain("Ok");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WithEmptyList()
    {
        var repoMock = new Mock<IServiceRepository>();
        repoMock.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Service>());

        var handler = new GetServicesQueryHandler(repoMock.Object);
        var result = await handler.Handle(new GetServicesQuery(), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
    }
}
