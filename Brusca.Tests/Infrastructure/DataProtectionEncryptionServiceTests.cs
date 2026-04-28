using Brusca.Core.Models;
using Brusca.Infrastructure.Encryption;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Brusca.Tests.Infrastructure;

public class DataProtectionEncryptionServiceTests
{
    private static DataProtectionEncryptionService NewService()
    {
        var provider = DataProtectionProvider.Create("Brusca.Tests");
        var opts = Options.Create(new BruscaOptions
        {
            Pii = new PiiOptions { DataProtectionApplicationName = "Brusca.Pii.Tests" }
        });
        return new DataProtectionEncryptionService(provider, opts);
    }

    [Fact]
    public void EncryptThenDecrypt_RoundTrips()
    {
        var svc = NewService();
        const string secret = "{\"name\":\"Jane Doe\",\"ssn\":\"123-45-6789\"}";

        var cipher = svc.Encrypt(secret);
        Assert.NotEqual(secret, cipher);
        Assert.NotEmpty(cipher);

        var clear = svc.Decrypt(cipher);
        Assert.Equal(secret, clear);
    }

    [Fact]
    public void EncryptEmpty_ReturnsEmpty()
    {
        var svc = NewService();
        Assert.Equal(string.Empty, svc.Encrypt(string.Empty));
        Assert.Equal(string.Empty, svc.Decrypt(string.Empty));
    }
}
