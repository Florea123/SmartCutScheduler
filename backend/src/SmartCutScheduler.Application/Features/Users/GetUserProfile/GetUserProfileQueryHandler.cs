using MediatR;
using SmartCutScheduler.Application.Common.Models;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Users.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetUserProfileQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return null;
        return new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.PhoneNumber,
            user.ProfilePictureUrl,
            user.FreshHaircutPhotoUrl,
            user.Role.ToString(),
            user.CreatedAtUtc
        );
    }
}
