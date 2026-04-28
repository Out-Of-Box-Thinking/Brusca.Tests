using Brusca.Core.Enums;
using Brusca.Core.Models;
using Brusca.Infrastructure.Pii;
using Microsoft.Extensions.Options;
using Xunit;

namespace Brusca.Tests.Infrastructure;

public class RegexPiiRedactionServiceTests
{
    private static RegexPiiRedactionService NewService(PiiOptions? overrideOpts = null)
    {
        var opts = new BruscaOptions { Pii = overrideOpts ?? new PiiOptions() };
        return new RegexPiiRedactionService(Options.Create(opts));
    }

    [Theory]
    [InlineData("Reach me at jane.doe@example.com today.", PiiKind.EmailAddress, "jane.doe@example.com")]
    [InlineData("SSN 123-45-6789 on file.",                 PiiKind.SocialSecurityNumber, "123-45-6789")]
    [InlineData("Card: 4111 1111 1111 1111",                PiiKind.CreditCardNumber, "4111 1111 1111 1111")]
    [InlineData("Dial (555) 123-4567 anytime.",             PiiKind.PhoneNumber, "(555) 123-4567")]
    [InlineData("Server is at 192.168.1.42 right now.",     PiiKind.IpAddress, "192.168.1.42")]
    public async Task RedactAsync_DetectsCommonKinds(string input, PiiKind expectedKind, string expectedValue)
    {
        var svc = NewService();

        var result = await svc.RedactAsync(input);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Segments, s =>
            s.Kind == expectedKind && s.Value.Trim() == expectedValue);
        Assert.DoesNotContain(expectedValue, result.Value.RedactedContent);
    }

    [Fact]
    public async Task RedactAsync_TokenIsStableAndOrdered()
    {
        var svc = NewService();
        var input = "Email a@b.com and also c@d.org now.";

        var r = await svc.RedactAsync(input);

        Assert.True(r.IsSuccess);
        Assert.Equal(2, r.Value.Segments.Count);
        Assert.Equal(1, r.Value.Segments[0].Ordinal);
        Assert.Equal(2, r.Value.Segments[1].Ordinal);
        Assert.Contains("[[PII:EmailAddress:0001]]", r.Value.RedactedContent);
        Assert.Contains("[[PII:EmailAddress:0002]]", r.Value.RedactedContent);
    }

    [Fact]
    public async Task RedactAsync_EmptyInput_NoSegments()
    {
        var svc = NewService();
        var r = await svc.RedactAsync(string.Empty);

        Assert.True(r.IsSuccess);
        Assert.Empty(r.Value.Segments);
        Assert.Equal(string.Empty, r.Value.RedactedContent);
    }

    [Fact]
    public async Task RedactAsync_DisabledDetector_DoesNotMatch()
    {
        var opts = new PiiOptions
        {
            Detectors = new PiiKindToggles { EmailAddress = false }
        };
        var svc = NewService(opts);

        var r = await svc.RedactAsync("Email me at x@y.com.");

        Assert.True(r.IsSuccess);
        Assert.DoesNotContain(r.Value.Segments, s => s.Kind == PiiKind.EmailAddress);
    }
}
