// Brusca.Tests/Infrastructure/FileExtensionServiceTests.cs
using Brusca.Core.Contracts.Repositories;
using Brusca.Core.Enums;
using Brusca.Core.Models.Extensions;
using Brusca.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace Brusca.Tests.Infrastructure;

public class FileExtensionServiceTests
{
    private readonly Mock<IFileExtensionRepository> _repo = new();

    private FileExtensionService CreateSut() => new(_repo.Object);

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".docx")]
    [InlineData(".xlsx")]
    [InlineData(".txt")]
    [InlineData(".json")]
    [InlineData(".csv")]
    public async Task GetUnknownExtensions_KnownExtension_ReturnsEmpty(string ext)
    {
        var result = await CreateSut().GetUnknownExtensionsAsync([ext]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotContain(ext);
    }

    [Fact]
    public async Task GetUnknownExtensions_UnknownExtension_ReturnsIt()
    {
        var result = await CreateSut().GetUnknownExtensionsAsync([".zzz_custom"]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(".zzz_custom");
    }

    [Fact]
    public async Task SyncFromScanAsync_NewExtensions_CallsBulkUpsert()
    {
        _repo
            .Setup(r => r.BulkUpsertAsync(It.IsAny<IEnumerable<FileExtensionRecord>>(), default))
            .ReturnsAsync(FluentResults.Result.Ok());

        var scan = new ExtensionScanResult
        {
            AllExtensions = [".pdf", ".docx", ".xyz"],
            CleaningId = Guid.NewGuid()
        };

        var result = await CreateSut().SyncFromScanAsync(scan);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r =>
            r.BulkUpsertAsync(
                It.Is<IEnumerable<FileExtensionRecord>>(recs => recs.Count() == 3),
                default),
            Times.Once);
    }

    [Fact]
    public async Task RegisterPackageForExtension_CallsRepoUpdateStatus()
    {
        _repo
            .Setup(r => r.UpdateStatusAsync(".xyz", FileExtensionStatus.PendingPackage, "XyzReader", default))
            .ReturnsAsync(FluentResults.Result.Ok());

        var result = await CreateSut().RegisterPackageForExtensionAsync(".xyz", "XyzReader");

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r =>
            r.UpdateStatusAsync(".xyz", FileExtensionStatus.PendingPackage, "XyzReader", default),
            Times.Once);
    }
}
