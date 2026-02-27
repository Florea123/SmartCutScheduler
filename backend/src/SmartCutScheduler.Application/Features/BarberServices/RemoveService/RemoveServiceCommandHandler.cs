using MediatR;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.BarberServices.RemoveService;

public class RemoveServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveServiceCommand, bool>
{
    public async Task<bool> Handle(RemoveServiceCommand request, CancellationToken cancellationToken)
    {
        var barber = await unitOfWork.Barbers.GetByIdAsync(request.BarberId, cancellationToken);
        if (barber == null)
        {
            throw new InvalidOperationException("Frizer nu a fost găsit.");
        }

        var barberService = barber.BarberServices.FirstOrDefault(bs => bs.ServiceId == request.ServiceId);
        if (barberService == null)
        {
            throw new InvalidOperationException("Serviciul nu a fost găsit.");
        }

        barber.BarberServices.Remove(barberService);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
