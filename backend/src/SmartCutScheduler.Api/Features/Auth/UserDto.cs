namespace SmartCutScheduler.Api.Features.Auth;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string? PhoneNumber,
    string? ProfilePictureUrl,
    string Role,
    DateTime CreatedAtUtc
);
