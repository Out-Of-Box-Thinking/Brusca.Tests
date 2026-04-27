// Brusca.Tests/Api/CleaningsControllerTests.cs
using Brusca.Api.DTOs.Request;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Brusca.Tests.Api;

/// <summary>
/// Integration tests using WebApplicationFactory.
/// These require a running SQL Server — skip in CI without DB by checking
/// an environment variable: BRUSCA_INTEGRATION_TESTS=true
/// </summary>
[Trait("Category", "Integration")]
public class CleaningsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly bool RunIntegration =
        Environment.GetEnvironmentVariable("BRUSCA_INTEGRATION_TESTS") == "true";

    public CleaningsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task Post_Cleanings_WithValidPath_Returns201()
    {
        Skip.IfNot(RunIntegration, "Set BRUSCA_INTEGRATION_TESTS=true to run.");

        var client = _factory.CreateClient();
        // In real tests, acquire a test JWT here
        // client.DefaultRequestHeaders.Authorization = new("Bearer", testJwt);

        var request = new StartCleaningRequest(@"C:\TestData", null);
        var response = await client.PostAsJsonAsync("/api/cleanings", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Get_Cleanings_UnknownId_Returns404()
    {
        Skip.IfNot(RunIntegration, "Set BRUSCA_INTEGRATION_TESTS=true to run.");

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/cleanings/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
