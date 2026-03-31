using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using SmartCutScheduler.Infrastructure.FileStorage;
using Xunit;

public class LocalFileStorageService_AllCases
{
    [Fact]
    public async Task SaveProfileImageAsync_ShouldSaveFile_AndReturnRelativePath()
    {
        var service = new LocalFileStorageService();
        var userId = Guid.NewGuid();
        var fileMock = new Mock<IFormFile>();
        var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        fileMock.Setup(f => f.FileName).Returns("test.png");
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).Returns((Stream s, CancellationToken c) => ms.CopyToAsync(s, c));
        var result = await service.SaveProfileImageAsync(userId, fileMock.Object, CancellationToken.None);
        result.Should().StartWith("/profile-images/");
        result.Should().Contain(userId.ToString());
        result.Should().EndWith(".png");
        // Clean up
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "profile-images", userId + ".png");
        if (File.Exists(filePath)) File.Delete(filePath);
    }
}
