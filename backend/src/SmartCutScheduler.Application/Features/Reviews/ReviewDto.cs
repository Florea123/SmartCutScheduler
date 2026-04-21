namespace SmartCutScheduler.Application.Features.Reviews;

public record ReviewDto(
    Guid Id,
    Guid BarberId,
    Guid UserId,
    string UserName,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
