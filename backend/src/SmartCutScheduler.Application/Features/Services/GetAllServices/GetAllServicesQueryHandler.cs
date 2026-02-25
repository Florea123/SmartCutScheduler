using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Services.GetAllServices;

public class GetAllServicesQueryHandler(IServiceRepository serviceRepository) 
    : IRequestHandler<GetAllServicesQuery, IResult>
{
    public async Task<IResult> Handle(GetAllServicesQuery request, CancellationToken cancellationToken)
    {
        var services = await serviceRepository.GetAllAsync(includeInactive: false, cancellationToken);
        
        var result = services.Select(s => new
        {
            s.Id,
            s.Name,
            s.Description,
            s.DurationMinutes,
            s.BasePrice
        }).ToList();

        return Results.Ok(result);
    }
}
