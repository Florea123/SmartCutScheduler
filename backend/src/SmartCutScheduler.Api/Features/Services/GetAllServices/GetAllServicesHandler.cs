using SmartCutScheduler.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace SmartCutScheduler.Api.Features.Services.GetAllServices;

public class GetAllServicesHandler(AppDbContext db) : IRequestHandler<GetAllServicesQuery, IResult>
{
    public async Task<IResult> Handle(GetAllServicesQuery request, CancellationToken ct)
    {
        var services = await db.Services
            .Where(s => s.IsActive)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                s.DurationMinutes,
                s.BasePrice
            })
            .ToListAsync(ct);

        return Results.Ok(services);
    }
}
