using MediatR;
using SmartCutScheduler.Application.Common.Models;

namespace SmartCutScheduler.Application.Features.Users.GetUserProfile;

public record GetUserProfileQuery(Guid UserId) : IRequest<UserDto?>;
