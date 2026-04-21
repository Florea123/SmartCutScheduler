using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SmartCutScheduler.Application.Features.Services.GetAllServices;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.Services;

public class GetAllServicesQueryHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldReturnOk_WithEmptyList_WhenNoServicesExist()
    {
        var repoMock = new Mock<IServiceRepository>();
        repoMock.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Service>());
        var handler = new GetAllServicesQueryHandler(repoMock.Object);

        var result = await handler.Handle(new GetAllServicesQuery(), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WithServicesList()
    {
        var serviceId = Guid.NewGuid();
        var service = new Service
        {
            Id = serviceId,
            Name = "Tuns",
            Description = "Classic tuns",
            DurationMinutes = 30,
            BasePrice = 40m,
            IsActive = true
        };
        var repoMock = new Mock<IServiceRepository>();
        repoMock.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Service> { service });
        var handler = new GetAllServicesQueryHandler(repoMock.Object);

        var result = await handler.Handle(new GetAllServicesQuery(), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
    }

    [Fact]
    public async Task Handle_ShouldOnlyReturnActiveServices()
    {
        // Handler passes includeInactive: false to repository — verify the call
        var repoMock = new Mock<IServiceRepository>();
        repoMock.Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Service>());
        var handler = new GetAllServicesQueryHandler(repoMock.Object);

        await handler.Handle(new GetAllServicesQuery(), CancellationToken.None);

        repoMock.Verify(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()), Times.Once);
    }
}
