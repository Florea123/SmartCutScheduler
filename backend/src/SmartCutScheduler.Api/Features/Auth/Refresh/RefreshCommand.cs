using MediatR;

namespace SmartCutScheduler.Api.Features.Auth.Refresh;

public record RefreshCommand : IRequest<IResult>;
