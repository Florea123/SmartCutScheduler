using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Application.Common.Interfaces;

namespace SmartCutScheduler.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "profile-images");
    private readonly string _freshCutPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fresh-haircut-photos");

    public async Task<string> SaveProfileImageAsync(Guid userId, IFormFile file, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{userId}{ext}";
        var filePath = Path.Combine(_basePath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }
        return $"/profile-images/{fileName}";
    }

    public async Task<string> SaveFreshHaircutPhotoAsync(Guid userId, IFormFile file, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_freshCutPath))
            Directory.CreateDirectory(_freshCutPath);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{userId}{ext}";
        var filePath = Path.Combine(_freshCutPath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }
        return $"/fresh-haircut-photos/{fileName}";
    }
}