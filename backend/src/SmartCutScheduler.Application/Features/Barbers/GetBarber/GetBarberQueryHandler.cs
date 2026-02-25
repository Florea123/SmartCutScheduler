using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Application.Common.Models;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Barbers.GetBarber;

public class GetBarberQueryHandler(IBarberRepository barberRepository) 
    : IRequestHandler<GetBarberQuery, IResult>
{
    public async Task<IResult> Handle(GetBarberQuery request, CancellationToken cancellationToken)
    {
        var barber = await barberRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (barber is null)
            return Results.NotFound("Barber not found.");

        var result = new
        {
            barber.Id,
            barber.Name,
            barber.Description,
            barber.PhotoUrl,
            barber.PhoneNumber,
            barber.Email,
            barber.IsActive,
            Services = barber.BarberServices.Select(bs => new ServiceDto(
                bs.Service.Id,
                bs.Service.Name,
                bs.Service.DurationMinutes,
                bs.CustomPrice ?? bs.Service.BasePrice
            )).ToList(),
            WorkSchedule = barber.WorkSchedules.Select(ws => new
            {
                ws.DayOfWeek,
                ws.StartTime,
                ws.EndTime,
                ws.IsWorkingDay
            }).ToList()
        };

        return Results.Ok(result);
    }
}
