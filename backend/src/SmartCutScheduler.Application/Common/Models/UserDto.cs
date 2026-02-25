namespace SmartCutScheduler.Application.Common.Models;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string? PhoneNumber,
    string? ProfilePictureUrl,
    string Role,
    DateTime CreatedAtUtc
);
