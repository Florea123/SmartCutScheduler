using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Application.Common.Interfaces;

namespace SmartCutScheduler.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "profile-images");

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
        // Return relative path for serving via static files
        return $"/profile-images/{fileName}";
    }
}