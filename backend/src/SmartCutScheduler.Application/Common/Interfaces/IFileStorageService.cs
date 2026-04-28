using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveProfileImageAsync(Guid userId, IFormFile file, CancellationToken cancellationToken);
    Task<string> SaveFreshHaircutPhotoAsync(Guid userId, IFormFile file, CancellationToken cancellationToken);
}