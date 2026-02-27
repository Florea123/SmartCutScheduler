using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Users.DeleteUser;

public class DeleteUserCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteUserCommand, IResult>
{
    public async Task<IResult> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken);

        if (user is null)
            return Results.NotFound("User not found.");

        await unitOfWork.Users.DeleteAsync(request.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = "User deleted successfully." });
    }
}
