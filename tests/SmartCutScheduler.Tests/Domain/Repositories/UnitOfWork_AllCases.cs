using Xunit;
using Moq;
using FluentAssertions;
using SmartCutScheduler.Domain.Repositories;
using System.Threading.Tasks;
using System.Threading;

public class UnitOfWork_AllCases
{
    [Fact]
    public async Task SaveChangesAsync_ShouldReturnInt()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var result = await unitOfWorkMock.Object.SaveChangesAsync();
        result.Should().Be(1);
    }

    [Fact]
    public async Task BeginCommitRollbackTransaction_ShouldNotThrow()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await unitOfWorkMock.Object.BeginTransactionAsync();
        await unitOfWorkMock.Object.CommitTransactionAsync();
        await unitOfWorkMock.Object.RollbackTransactionAsync();
        unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
