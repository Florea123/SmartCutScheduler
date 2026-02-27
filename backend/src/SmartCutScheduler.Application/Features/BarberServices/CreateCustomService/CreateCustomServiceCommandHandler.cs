using MediatR;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.BarberServices.CreateCustomService;

public class CreateCustomServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCustomServiceCommand, Guid>
{
    public async Task<Guid> Handle(CreateCustomServiceCommand request, CancellationToken cancellationToken)
    {
        // Verifică dacă frizerul există
        var barber = await unitOfWork.Barbers.GetByIdAsync(request.BarberId, cancellationToken);
        if (barber == null)
        {
            throw new InvalidOperationException("Frizer nu a fost găsit.");
        }

        // Creează serviciul nou
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            DurationMinutes = request.DurationMinutes,
            BasePrice = request.Price,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await unitOfWork.Services.AddAsync(service, cancellationToken);

        // Asociază serviciul cu frizerul
        var barberService = new BarberService
        {
            BarberId = request.BarberId,
            ServiceId = service.Id,
            CustomPrice = null // folosim prețul de bază stabilit de frizer
        };

        barber.BarberServices.Add(barberService);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return service.Id;
    }
}
