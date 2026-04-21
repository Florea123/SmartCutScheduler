using Xunit;
using Moq;
using FluentAssertions;
using SmartCutScheduler.Domain.Repositories;
using System.Threading.Tasks;
using System.Threading;

public class UnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_ShouldReturnInt()
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await unitOfWorkMock.Object.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
    }

    // Alte teste pentru BeginTransactionAsync, CommitTransactionAsync, RollbackTransactionAsync
}
