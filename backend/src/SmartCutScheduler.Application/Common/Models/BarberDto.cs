namespace SmartCutScheduler.Application.Common.Models;

public record BarberDto(
    Guid Id,
    string Name,
    string? Description,
    string? PhotoUrl,
    string? PhoneNumber,
    string? Email,
    bool IsActive,
    List<ServiceDto> Services
);

public record ServiceDto(
    Guid Id,
    string Name,
    int DurationMinutes,
    decimal Price
);
