using MediatR;
using Microsoft.AspNetCore.Http;

using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Application.Common.Interfaces;

namespace SmartCutScheduler.Application.Features.Users.UpdateUserProfile;

public class UpdateUserProfileCommandHandler(
    IUnitOfWork unitOfWork,
    IFileStorageService fileStorageService
) : IRequestHandler<UpdateUserProfileCommand, bool>
{
    public async Task<bool> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        user.Name = request.Name;
        user.PhoneNumber = request.PhoneNumber;
        user.UpdatedAtUtc = DateTime.UtcNow;

        if (user.Role == UserRole.Barber)
            await UpdateBarberDataAsync(request, user, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task UpdateBarberDataAsync(UpdateUserProfileCommand request, User user, CancellationToken cancellationToken)
    {
        var barber = await unitOfWork.Barbers.GetByIdAsync(user.Id, cancellationToken);
        if (barber == null)
            return;

        if (request.Description != null)
        {
            barber.Description = request.Description;
            barber.UpdatedAtUtc = DateTime.UtcNow;
        }

        if (request.ProfileImage != null)
        {
            var imageUrl = await fileStorageService.SaveProfileImageAsync(user.Id, request.ProfileImage, cancellationToken);
            user.ProfilePictureUrl = imageUrl;
            barber.PhotoUrl = imageUrl;
        }
        else
        {
            SyncPhotos(user, barber);
        }
    }

    private static void SyncPhotos(User user, Domain.Entities.Barber barber)
    {
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl) && barber.PhotoUrl != user.ProfilePictureUrl)
            barber.PhotoUrl = user.ProfilePictureUrl;
        else if (!string.IsNullOrEmpty(barber.PhotoUrl) && user.ProfilePictureUrl != barber.PhotoUrl)
            user.ProfilePictureUrl = barber.PhotoUrl;
    }
}