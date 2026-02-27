using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Users.GetAllUsers;

public record GetAllUsersQuery : IRequest<IResult>;
