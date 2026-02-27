using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Barbers.GetBarberWorkSchedule;

public record GetBarberWorkScheduleQuery(Guid BarberId) : IRequest<IResult>;

public class GetBarberWorkScheduleQueryHandler : IRequestHandler<GetBarberWorkScheduleQuery, IResult>
{
    public Task<IResult> Handle(GetBarberWorkScheduleQuery request, CancellationToken cancellationToken)
    {
        // Hardcoded work schedule: Monday-Friday 08:00-18:00
        var schedules = new List<WorkScheduleDto>
        {
            new() { DayOfWeek = 1, DayName = "Monday", StartTime = "08:00", EndTime = "18:00", IsWorkingDay = true },
            new() { DayOfWeek = 2, DayName = "Tuesday", StartTime = "08:00", EndTime = "18:00", IsWorkingDay = true },
            new() { DayOfWeek = 3, DayName = "Wednesday", StartTime = "08:00", EndTime = "18:00", IsWorkingDay = true },
            new() { DayOfWeek = 4, DayName = "Thursday", StartTime = "08:00", EndTime = "18:00", IsWorkingDay = true },
            new() { DayOfWeek = 5, DayName = "Friday", StartTime = "08:00", EndTime = "18:00", IsWorkingDay = true },
            new() { DayOfWeek = 6, DayName = "Saturday", StartTime = "00:00", EndTime = "00:00", IsWorkingDay = false },
            new() { DayOfWeek = 7, DayName = "Sunday", StartTime = "00:00", EndTime = "00:00", IsWorkingDay = false }
        };

        return Task.FromResult(Results.Ok(schedules));
    }
}

public class WorkScheduleDto
{
    public Guid Id { get; set; }
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public bool IsWorkingDay { get; set; }
}
