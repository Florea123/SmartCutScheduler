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
        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            throw new Exception("User not found");

        user.Name = request.Name;
        user.PhoneNumber = request.PhoneNumber;
        user.UpdatedAtUtc = DateTime.UtcNow;

        if (user.Role == UserRole.Barber && request.Description != null)
        {
            var barber = await unitOfWork.Barbers.GetByIdAsync(request.UserId, cancellationToken);
            if (barber != null)
            {
                barber.Description = request.Description;
                barber.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        if (request.ProfileImage != null)
        {
            var imageUrl = await fileStorageService.SaveProfileImageAsync(request.UserId, request.ProfileImage, cancellationToken);
            user.ProfilePictureUrl = imageUrl;
            if (user.Role == UserRole.Barber)
            {
                var barber = await unitOfWork.Barbers.GetByIdAsync(request.UserId, cancellationToken);
                if (barber != null)
                {
                    barber.PhotoUrl = imageUrl;
                    // Sincronizează și invers dacă imaginea a fost schimbată din altă parte
                    if (barber.PhotoUrl != user.ProfilePictureUrl)
                        user.ProfilePictureUrl = barber.PhotoUrl;
                }
            }
        }
        // Dacă userul e frizer și are imagine doar pe una din entități, sincronizează-le
        if (user.Role == UserRole.Barber)
        {
            var barber = await unitOfWork.Barbers.GetByIdAsync(request.UserId, cancellationToken);
            if (barber != null)
            {
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl) && barber.PhotoUrl != user.ProfilePictureUrl)
                    barber.PhotoUrl = user.ProfilePictureUrl;
                if (!string.IsNullOrEmpty(barber.PhotoUrl) && user.ProfilePictureUrl != barber.PhotoUrl)
                    user.ProfilePictureUrl = barber.PhotoUrl;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}