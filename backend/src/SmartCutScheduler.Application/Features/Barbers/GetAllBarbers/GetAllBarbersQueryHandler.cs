using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Application.Common.Models;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Barbers.GetAllBarbers;

public class GetAllBarbersQueryHandler(IBarberRepository barberRepository) 
    : IRequestHandler<GetAllBarbersQuery, IResult>
{
    public async Task<IResult> Handle(GetAllBarbersQuery request, CancellationToken cancellationToken)
    {
        var barbers = await barberRepository.GetAllAsync(includeInactive: false, cancellationToken);
        
        var result = barbers.Select(b => new BarberDto(
            b.Id,
            b.Name,
            b.Description,
            b.PhotoUrl,
            b.PhoneNumber,
            b.Email,
            b.IsActive,
            b.BarberServices.Select(bs => new ServiceDto(
                bs.Service.Id,
                bs.Service.Name,
                bs.Service.DurationMinutes,
                bs.CustomPrice ?? bs.Service.BasePrice
            )).ToList()
        )).ToList();

        return Results.Ok(result);
    }
}
