using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Auth.Refresh;

public record RefreshCommand : IRequest<IResult>;
