using MediatR;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.BarberServices.AddService;

public class AddServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<AddServiceCommand, Guid>
{
    public async Task<Guid> Handle(AddServiceCommand request, CancellationToken cancellationToken)
    {
        // Verifică dacă frizerul există
        var barber = await unitOfWork.Barbers.GetByIdAsync(request.BarberId, cancellationToken);
        if (barber == null)
        {
            throw new InvalidOperationException("Frizer nu a fost găsit.");
        }

        // Verifică dacă serviciul există
        var service = await unitOfWork.Services.GetByIdAsync(request.ServiceId, cancellationToken);
        if (service == null)
        {
            throw new InvalidOperationException("Serviciul nu a fost găsit.");
        }

        // Verifică dacă frizerul deja oferă acest serviciu
        if (barber.BarberServices.Any(bs => bs.ServiceId == request.ServiceId))
        {
            throw new InvalidOperationException("Acest serviciu este deja adăugat.");
        }

        // Adaugă serviciul
        var barberService = new BarberService
        {
            BarberId = request.BarberId,
            ServiceId = request.ServiceId,
            CustomPrice = request.CustomPrice
        };

        barber.BarberServices.Add(barberService);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return request.ServiceId;
    }
}
