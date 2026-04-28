using Brusca.Core.Enums;
using Brusca.Infrastructure.Pii;
using Xunit;

namespace Brusca.Tests.Infrastructure;

public class HeuristicDocumentTypeClassifierTests
{
    private readonly HeuristicDocumentTypeClassifier _classifier = new();

    [Theory]
    [InlineData(".jpg",  DocumentType.Photo)]
    [InlineData(".mp3",  DocumentType.Audio)]
    [InlineData(".mp4",  DocumentType.Video)]
    [InlineData(".xlsx", DocumentType.Spreadsheet)]
    [InlineData(".cs",   DocumentType.SourceCode)]
    [InlineData(".zip",  DocumentType.Archive)]
    public async Task ClassifyAsync_MapsExtensionToDocumentType(string ext, DocumentType expected)
    {
        var r = await _classifier.ClassifyAsync(string.Empty, ext);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    [Theory]
    [InlineData("Invoice number: 12345 Amount due: $100", DocumentType.Invoice)]
    [InlineData("Patient diagnosis: hypertension. Prescription enclosed.", DocumentType.MedicalRecord)]
    [InlineData("Curriculum vitae of John (redacted). Work experience...", DocumentType.Resume)]
    [InlineData("This contract is entered into between the party of the first part...", DocumentType.Contract)]
    public async Task ClassifyAsync_KeywordsDetectDocumentType(string content, DocumentType expected)
    {
        var r = await _classifier.ClassifyAsync(content, ".txt");
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }
}
