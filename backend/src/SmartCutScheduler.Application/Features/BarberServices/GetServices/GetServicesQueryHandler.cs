using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.BarberServices.GetServices;

public class GetServicesQueryHandler(IServiceRepository serviceRepository)
    : IRequestHandler<GetServicesQuery, IResult>
{
    public async Task<IResult> Handle(GetServicesQuery request, CancellationToken cancellationToken)
    {
        var services = await serviceRepository.GetAllAsync(false, cancellationToken);
        
        var result = services.Select(s => new
        {
            s.Id,
            s.Name,
            s.Description,
            s.DurationMinutes,
            BasePrice = s.BasePrice,
            s.IsActive
        }).ToList();

        return Results.Ok(result);
    }
}
