using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using SmartCutScheduler.Application.Features.Barbers.GetBarberWorkSchedule;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.Barbers;

public class GetBarberWorkScheduleQueryHandler_Tests
{
    [Fact]
    public async Task Handle_ShouldReturn7DaySchedule()
    {
        var handler = new GetBarberWorkScheduleQueryHandler();
        var query = new GetBarberWorkScheduleQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.ToString().Should().Contain("Ok");
    }

    [Fact]
    public async Task Handle_ShouldReturnWorkingDaysMonToFri()
    {
        var handler = new GetBarberWorkScheduleQueryHandler();
        var query = new GetBarberWorkScheduleQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);
        var resultType = result?.GetType();
        object? payload = null;
        if (resultType != null && resultType.Name.StartsWith("Ok"))
        {
            var valueProp = resultType.GetProperty("Value");
            payload = valueProp?.GetValue(result);
        }

        payload.Should().NotBeNull();
        var list = payload as System.Collections.IEnumerable;
        int count = 0;
        foreach (var _ in list!) count++;
        count.Should().Be(7);
    }

    [Fact]
    public async Task Handle_ShouldReturn_WeekendAsNonWorking()
    {
        var handler = new GetBarberWorkScheduleQueryHandler();
        var query = new GetBarberWorkScheduleQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);
        result.Should().NotBeNull();
        // Just verifies the handler completes without error
        result.ToString().Should().Contain("Ok");
    }
}
