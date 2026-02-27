using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Barbers.CreateBarber;

public class CreateBarberCommandHandler(
    IUnitOfWork unitOfWork,
    PasswordHasher<User> passwordHasher
) : IRequestHandler<CreateBarberCommand, Guid>
{
    public async Task<Guid> Handle(CreateBarberCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        var existingUser = await unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email-ul este deja folosit.");
        }

        // Generate a single ID to use for both User and Barber
        var userId = Guid.NewGuid();

        // Create User account for barber
        var user = new User
        {
            Id = userId,
            Name = request.Name,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Role = UserRole.Barber,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        
        await unitOfWork.Users.AddAsync(user, cancellationToken);

        // Create Barber entity with same ID as User
        var barber = new Barber
        {
            Id = userId,
            Name = request.Name,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Description = request.Description,
            PhotoUrl = request.PhotoUrl,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await unitOfWork.Barbers.AddAsync(barber, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return barber.Id;
    }
}
