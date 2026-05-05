using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SmartCutScheduler.Application.Common.Interfaces;

namespace SmartCutScheduler.Infrastructure.FileStorage;

public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _serviceClient;
    private readonly AzureBlobStorageOptions _options;

    public AzureBlobStorageService(BlobServiceClient serviceClient, IOptions<AzureBlobStorageOptions> options)
    {
        _serviceClient = serviceClient;
        _options = options.Value;
    }

    public async Task<string> SaveProfileImageAsync(Guid userId, IFormFile file, CancellationToken cancellationToken)
    {
        return await UploadToBlobAsync(_options.ProfileImagesContainer, userId, file, cancellationToken);
    }

    public async Task<string> SaveFreshHaircutPhotoAsync(Guid userId, IFormFile file, CancellationToken cancellationToken)
    {
        return await UploadToBlobAsync(_options.FreshHaircutPhotosContainer, userId, file, cancellationToken);
    }

    private async Task<string> UploadToBlobAsync(
        string containerName,
        Guid userId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var containerClient = _serviceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var ext = Path.GetExtension(file.FileName);
        var blobName = $"{userId}{ext}";
        var blobClient = containerClient.GetBlobClient(blobName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);

        return blobClient.Uri.ToString();
    }
}
