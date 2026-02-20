using SmartCutScheduler.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace SmartCutScheduler.Api.Features.Barbers.GetAllBarbers;

public class GetAllBarbersHandler(AppDbContext db) : IRequestHandler<GetAllBarbersQuery, IResult>
{
    public async Task<IResult> Handle(GetAllBarbersQuery request, CancellationToken ct)
    {
        var barbers = await db.Barbers
            .Where(b => b.IsActive)
            .Include(b => b.BarberServices)
            .ThenInclude(bs => bs.Service)
            .Select(b => new BarberDto(
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
            ))
            .ToListAsync(ct);

        return Results.Ok(barbers);
    }
}
