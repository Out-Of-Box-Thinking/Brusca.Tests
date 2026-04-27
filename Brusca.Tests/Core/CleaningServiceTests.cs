// Brusca.Tests/Core/CleaningServiceTests.cs
using Brusca.Core.Contracts.Logging;
using Brusca.Core.Contracts.Repositories;
using Brusca.Core.Contracts.Services;
using Brusca.Core.Enums;
using Brusca.Core.Models.Cleaning;
using Brusca.Core.Models.Extensions;
using Brusca.Infrastructure.Claude;
using Brusca.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace Brusca.Tests.Core;

public class CleaningServiceTests
{
    private readonly Mock<ICleaningRepository> _cleaningRepo = new();
    private readonly Mock<IPromptStepRepository> _promptRepo = new();
    private readonly Mock<IFileSystemService> _fs = new();
    private readonly Mock<IFileExtensionService> _extService = new();
    private readonly Mock<ClaudePromptService> _claude = new();
    private readonly Mock<IAuditLogger> _audit = new();
    private readonly Mock<IErrorLogger> _log = new();

    private ICleaningService CreateSut() => new CleaningService(
        _cleaningRepo.Object, _promptRepo.Object,
        _fs.Object, _extService.Object,
        _claude.Object, _audit.Object, _log.Object);

    [Fact]
    public async Task StartCleaningAsync_ValidPath_ReturnsCleaning()
    {
        // Arrange
        var expected = new Cleaning { RootPath = @"C:\test", CreatedByUserId = "user1" };
        _cleaningRepo
            .Setup(r => r.CreateAsync(It.IsAny<Cleaning>(), default))
            .ReturnsAsync(FluentResults.Result.Ok(expected));

        _audit
            .Setup(a => a.LogAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                null, null, default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateSut().StartCleaningAsync(@"C:\test", "user1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RootPath.Should().Be(@"C:\test");
    }

    [Fact]
    public async Task ScanExtensionsAsync_UnknownExtensions_SetsAwaitingStatus()
    {
        // Arrange
        var cleaningId = Guid.NewGuid();
        var cleaning = new Cleaning { Id = cleaningId, RootPath = @"C:\test" };

        _cleaningRepo
            .Setup(r => r.GetByIdAsync(cleaningId, default))
            .ReturnsAsync(FluentResults.Result.Ok(cleaning));
        _cleaningRepo
            .Setup(r => r.UpdateStatusAsync(cleaningId, It.IsAny<CleaningStatus>(), default))
            .ReturnsAsync(FluentResults.Result.Ok());
        _cleaningRepo
            .Setup(r => r.AddFileExtensionsAsync(cleaningId, It.IsAny<IEnumerable<CleaningFileExtension>>(), default))
            .ReturnsAsync(FluentResults.Result.Ok());

        var scanResult = new ExtensionScanResult
        {
            CleaningId = cleaningId,
            AllExtensions = [".pdf", ".abc"],
            UnknownExtensions = [".abc"],
            TotalFileCount = 10
        };

        _fs
            .Setup(f => f.ScanForExtensionsAsync(@"C:\test", cleaningId, default))
            .ReturnsAsync(FluentResults.Result.Ok(scanResult));
        _extService
            .Setup(e => e.SyncFromScanAsync(scanResult, default))
            .ReturnsAsync(FluentResults.Result.Ok());

        // Act
        var result = await CreateSut().ScanExtensionsAsync(cleaningId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UnknownExtensions.Should().Contain(".abc");

        _cleaningRepo.Verify(r =>
            r.UpdateStatusAsync(cleaningId, CleaningStatus.AwaitingExtensionResolution, default),
            Times.Once);
    }
}
