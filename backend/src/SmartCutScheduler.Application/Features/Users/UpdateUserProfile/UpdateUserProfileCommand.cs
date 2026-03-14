using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Users.UpdateUserProfile;

public class UpdateUserProfileCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Description { get; set; }
    public IFormFile? ProfileImage { get; set; }
}