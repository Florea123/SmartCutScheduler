using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Barbers.DeleteBarber;

public class DeleteBarberCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteBarberCommand, IResult>
{
    public async Task<IResult> Handle(DeleteBarberCommand request, CancellationToken cancellationToken)
    {
        var barber = await unitOfWork.Barbers.GetByIdAsync(request.Id, cancellationToken);

        if (barber is null)
            return Results.NotFound("Barber not found.");

        await unitOfWork.Barbers.DeleteAsync(request.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = "Barber deleted successfully." });
    }
}
