using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.BarberServices.GetMyServices;

public class GetMyServicesQueryHandler(IBarberRepository barberRepository)
    : IRequestHandler<GetMyServicesQuery, IResult>
{
    public async Task<IResult> Handle(GetMyServicesQuery request, CancellationToken cancellationToken)
    {
        var barber = await barberRepository.GetByIdAsync(request.BarberId, cancellationToken);
        
        if (barber == null)
        {
            return Results.NotFound(new { message = "Frizer nu a fost găsit." });
        }

        var services = barber.BarberServices.Select(bs => new
        {
            ServiceId = bs.ServiceId,
            Name = bs.Service.Name,
            Description = bs.Service.Description,
            DurationMinutes = bs.Service.DurationMinutes,
            BasePrice = bs.Service.BasePrice,
            CustomPrice = bs.CustomPrice,
            FinalPrice = bs.CustomPrice ?? bs.Service.BasePrice
        }).ToList();

        return Results.Ok(services);
    }
}
