using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Users.UpdateFreshHaircutPhoto;

public class UpdateFreshHaircutPhotoCommand : IRequest<string>
{
    public Guid UserId { get; set; }
    public IFormFile Photo { get; set; } = default!;
}
