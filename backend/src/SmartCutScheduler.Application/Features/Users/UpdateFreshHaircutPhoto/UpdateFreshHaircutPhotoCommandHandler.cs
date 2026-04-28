using MediatR;
using SmartCutScheduler.Application.Common.Interfaces;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Users.UpdateFreshHaircutPhoto;

public class UpdateFreshHaircutPhotoCommandHandler(
    IUnitOfWork unitOfWork,
    IFileStorageService fileStorageService
) : IRequestHandler<UpdateFreshHaircutPhotoCommand, string>
{
    public async Task<string> Handle(UpdateFreshHaircutPhotoCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"User {request.UserId} not found.");

        var photoUrl = await fileStorageService.SaveFreshHaircutPhotoAsync(
            request.UserId, request.Photo, cancellationToken);

        user.FreshHaircutPhotoUrl = photoUrl;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return photoUrl;
    }
}
