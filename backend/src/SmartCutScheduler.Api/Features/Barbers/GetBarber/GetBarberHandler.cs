using SmartCutScheduler.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace SmartCutScheduler.Api.Features.Barbers.GetBarber;

public class GetBarberHandler(AppDbContext db) : IRequestHandler<GetBarberQuery, IResult>
{
    public async Task<IResult> Handle(GetBarberQuery request, CancellationToken ct)
    {
        var barber = await db.Barbers
            .Where(b => b.Id == request.Id && b.IsActive)
            .Include(b => b.BarberServices)
            .ThenInclude(bs => bs.Service)
            .Include(b => b.WorkSchedules)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Description,
                b.PhotoUrl,
                b.PhoneNumber,
                b.Email,
                b.IsActive,
                Services = b.BarberServices.Select(bs => new ServiceDto(
                    bs.Service.Id,
                    bs.Service.Name,
                    bs.Service.DurationMinutes,
                    bs.CustomPrice ?? bs.Service.BasePrice
                )).ToList(),
                WorkSchedule = b.WorkSchedules.Select(ws => new
                {
                    ws.DayOfWeek,
                    ws.StartTime,
                    ws.EndTime,
                    ws.IsWorkingDay
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (barber is null)
            return Results.NotFound("Barber not found.");

        return Results.Ok(barber);
    }
}
