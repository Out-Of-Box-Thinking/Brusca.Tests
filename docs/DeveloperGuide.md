# Developer Guide — Brusca.Tests

For engineers adding or maintaining tests across the Brusca platform.

---

## 1. Test categories

The test project mixes three categories. Tag the class with `[Trait("Category", "...")]`:

| Category | Trait value | Runs in CI? |
|----------|-------------|-------------|
| Pure unit (mocks only) | `"Unit"`        | Yes, every PR. |
| Database integration   | `"Integration"` | Yes, on the gated job that has a SQL container. |
| API end-to-end         | `"E2E"`         | Yes, on the gated job. |

```powershell
dotnet test --filter "Category=Unit"
```

---

## 2. PII pipeline tests

The three new test classes form a contract for the privacy guarantees:

### `RegexPiiRedactionServiceTests`

- Confirms every detector in `PiiKindToggles` produces a `PiiSegment`.
- Confirms tokens have the form `[[PII:Kind:NNNN]]` and Ordinals are stable left-to-right.
- Confirms disabled detectors emit zero segments (so deployments can scope detection per region).

### `HeuristicDocumentTypeClassifierTests`

- Confirms `.jpg` → `Photo`, `.mp3` → `Audio`, `.cs` → `SourceCode`, etc.
- Confirms keyword cues route ambiguous text files (`.txt`, `.pdf`) to the right `DocumentType`.

### `DataProtectionEncryptionServiceTests`

- Confirms `Encrypt → Decrypt` is lossless.
- Confirms empty inputs short-circuit (i.e. the empty PII case never produces ciphertext).

If you change any contract — add a detector, change token format, change the encryption envelope — update the matching test class first.

---

## 3. Adding a controller integration test

Place the file under `Brusca.Tests/Api/`. Use `WebApplicationFactory<Program>`. Replace any external dependency (Claude, SQL) with mocks via `services.Replace(...)` in the factory's `ConfigureWebHost`:

```csharp
public class RedactEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RedactEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton<IClaudeStructureService>(_ =>
                {
                    var mock = new Mock<IClaudeStructureService>();
                    mock.Setup(m => m.AnalyzeStructureAsync(
                            It.IsAny<Guid>(),
                            It.IsAny<IReadOnlyList<DocumentTypeSummary>>(),
                            It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new DirectoryStructurePlan { Summary = "stub" });
                    return mock.Object;
                }));
            });
        }).CreateClient();
    }
}
```

---

## 4. Common pitfalls

1. `Program` not visible: add `public partial class Program {}` to the bottom of `Brusca.Api/Program.cs`.
2. Local NuGet feed missing: `New-Item -ItemType Directory -Path \\OOBT-NAS\Workstation\Repo\nupkgs -Force` and pack `Brusca.Core` + `Brusca.Infrastructure` first.
3. Pinned versions out of sync: every consumer (`Brusca.Api`, `Brusca.Tests`) must reference the same `Brusca.Core` / `Brusca.Infrastructure` version (e.g. `1.0.999-pii`). Mismatch produces strange `MissingMethodException` failures at test run.
