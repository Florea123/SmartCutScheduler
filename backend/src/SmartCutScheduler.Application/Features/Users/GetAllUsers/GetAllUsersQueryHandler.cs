using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Users.GetAllUsers;

public class GetAllUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetAllUsersQuery, IResult>
{
    public async Task<IResult> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);

        var result = users.Select(u => new
        {
            u.Id,
            u.Name,
            u.Email,
            u.PhoneNumber,
            Role = u.Role.ToString(),
            u.CreatedAtUtc
        }).ToList();

        return Results.Ok(result);
    }
}
