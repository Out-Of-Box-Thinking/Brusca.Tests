# Install Guide — Brusca.Tests

xUnit test project for the Brusca platform.

---

## 1. Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 9.0+ |
| The four sibling repos restored and building | `Brusca.Core`, `Brusca.Infrastructure`, `Brusca.Api`, `Brusca.Web` (Brusca.Web optional for unit tests) |

The csproj uses both NuGet feed packages **and** a `ProjectReference` to `Brusca.Api` (required by `WebApplicationFactory<Program>`):

```xml
<PackageReference Include="Brusca.Core" Version="1.0.999-pii" />
<PackageReference Include="Brusca.Infrastructure" Version="1.0.999-pii" />
<ProjectReference Include="..\..\Brusca.Api\Brusca.Api\Brusca.Api.csproj" />
```

---

## 2. Setup

```powershell
cd \\OOBT-NAS\Workstation\Repo\Brusca.Tests
dotnet restore Brusca.Tests/Brusca.Tests.csproj --force
dotnet build   Brusca.Tests/Brusca.Tests.csproj -c Debug
```

If `Brusca.Api` fails to build first, fix that repo first (see `Brusca.Api/docs/InstallGuide.md`).

---

## 3. Running the tests

```powershell
dotnet test Brusca.Tests/Brusca.Tests.csproj --logger "console;verbosity=normal"
```

Filter to PII-pipeline tests only:

```powershell
dotnet test --filter "FullyQualifiedName~Infrastructure.RegexPiiRedactionServiceTests|FullyQualifiedName~Infrastructure.HeuristicDocumentTypeClassifierTests|FullyQualifiedName~Infrastructure.DataProtectionEncryptionServiceTests"
```

---

## 4. Test layout

```
Brusca.Tests/
├── Api/
│   └── CleaningsControllerTests.cs              (integration via WebApplicationFactory)
└── Infrastructure/
    ├── RegexPiiRedactionServiceTests.cs         (NEW)
    ├── HeuristicDocumentTypeClassifierTests.cs  (NEW)
    └── DataProtectionEncryptionServiceTests.cs  (NEW)
```

---

## 5. Notes

- The integration test `CleaningsControllerTests` uses `WebApplicationFactory<Program>`. For that to work the `Program` class in `Brusca.Api` must be partial and visible to the test assembly. If the build fails with "`Program` is inaccessible", add `[assembly: InternalsVisibleTo("Brusca.Tests")]` to `Brusca.Api/Program.cs` or convert `Program` to a `public partial class Program {}`.
- The integration test depends on `SkippableFact` if you want CI to skip when an external dependency is unavailable. If that package is not referenced, replace `[SkippableFact]` with `[Fact]` (and remove the skip-condition logic).
