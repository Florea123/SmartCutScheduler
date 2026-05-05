using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using SmartCutScheduler.Infrastructure.FileStorage;
using Xunit;

public class AzureBlobStorageService_Tests
{
    private static AzureBlobStorageService CreateService(
        Mock<BlobServiceClient> serviceClientMock,
        string profileContainer = "profile-images",
        string freshCutContainer = "fresh-haircut-photos")
    {
        var options = Options.Create(new AzureBlobStorageOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            ProfileImagesContainer = profileContainer,
            FreshHaircutPhotosContainer = freshCutContainer
        });
        return new AzureBlobStorageService(serviceClientMock.Object, options);
    }

    private static Mock<IFormFile> CreateFileMock(string fileName = "photo.png")
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));
        return fileMock;
    }

    private static (Mock<BlobServiceClient> serviceClientMock, Mock<BlobContainerClient> containerClientMock, Mock<BlobClient> blobClientMock)
        SetupBlobMocks(Uri blobUri)
    {
        var blobClientMock = new Mock<BlobClient>();
        blobClientMock.Setup(b => b.Uri).Returns(blobUri);
        blobClientMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        var serviceClientMock = new Mock<BlobServiceClient>();
        serviceClientMock
            .Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(containerClientMock.Object);

        return (serviceClientMock, containerClientMock, blobClientMock);
    }

    [Fact]
    public async Task SaveProfileImageAsync_ShouldReturnBlobUri()
    {
        var blobUri = new Uri("https://account.blob.core.windows.net/profile-images/user.png");
        var (serviceClientMock, containerClientMock, _) = SetupBlobMocks(blobUri);

        var service = CreateService(serviceClientMock);
        var userId = Guid.NewGuid();
        var fileMock = CreateFileMock("photo.png");

        var result = await service.SaveProfileImageAsync(userId, fileMock.Object, CancellationToken.None);

        result.Should().Be(blobUri.ToString());
        serviceClientMock.Verify(s => s.GetBlobContainerClient("profile-images"), Times.Once);
    }

    [Fact]
    public async Task SaveFreshHaircutPhotoAsync_ShouldReturnBlobUri()
    {
        var blobUri = new Uri("https://account.blob.core.windows.net/fresh-haircut-photos/user.jpg");
        var (serviceClientMock, _, _) = SetupBlobMocks(blobUri);

        var service = CreateService(serviceClientMock);
        var userId = Guid.NewGuid();
        var fileMock = CreateFileMock("cut.jpg");

        var result = await service.SaveFreshHaircutPhotoAsync(userId, fileMock.Object, CancellationToken.None);

        result.Should().Be(blobUri.ToString());
        serviceClientMock.Verify(s => s.GetBlobContainerClient("fresh-haircut-photos"), Times.Once);
    }

    [Fact]
    public async Task SaveProfileImageAsync_BlobNameShouldContainUserIdAndExtension()
    {
        var blobUri = new Uri("https://account.blob.core.windows.net/profile-images/test.png");
        var (serviceClientMock, containerClientMock, _) = SetupBlobMocks(blobUri);

        var service = CreateService(serviceClientMock);
        var userId = Guid.NewGuid();
        var fileMock = CreateFileMock("anything.png");

        await service.SaveProfileImageAsync(userId, fileMock.Object, CancellationToken.None);

        containerClientMock.Verify(c => c.GetBlobClient($"{userId}.png"), Times.Once);
    }
}
