# Brusca.Tests

xUnit test suite for the Brusca AI-powered file organizer. Contains unit tests for Core services, infrastructure services, and API controller integration tests using `Microsoft.AspNetCore.Mvc.Testing`.

---

## Test structure

| Folder | What is tested |
|--------|----------------|
| `Core/` | `CleaningServiceTests` — pure domain logic, no I/O |
| `Infrastructure/` | `FileExtensionServiceTests` — infrastructure services with Moq stubs |
| `Api/` | `CleaningsControllerTests` — full HTTP round-trips via `WebApplicationFactory<Program>` |

---

## Dependencies

| Package / Reference | Purpose |
|--------------------|---------|
| `Brusca.Core` (NuGet) | Domain models and interfaces |
| `Brusca.Infrastructure` (NuGet) | Infrastructure implementations |
| `Brusca.Api` (**ProjectReference**) | Required by `WebApplicationFactory<Program>` — see checkout layout below |
| `xunit` | Test framework |
| `Moq` | Mocking |
| `FluentAssertions` | Assertion DSL |
| `Bogus` | Test data generation |
| `Microsoft.AspNetCore.Mvc.Testing` | In-process API test server |

---

## Checkout layout

`Microsoft.AspNetCore.Mvc.Testing` needs the compiled API entry-point assembly, so `Brusca.Api` is referenced as a **project reference** using a relative path. Both repos **must be checked out as siblings** under the same parent directory:

```
Workstation\Repo\
  Brusca.Core\          ← pack with pack.ps1 first
  Brusca.Infrastructure\ ← pack with pack.ps1 second
  Brusca.Api\           ← must be present for the project reference to resolve
  Brusca.Tests\         ← this repo
  nupkgs\               ← shared local NuGet feed (auto-created by pack.ps1)
```

---

## Local development setup

### 1. Pack upstream libraries

```powershell
cd ..\Brusca.Core;           .\pack.ps1 -Version 1.0.0
cd ..\Brusca.Infrastructure; .\pack.ps1 -Version 1.0.0
```

### 2. Restore and run tests

```bash
dotnet restore
dotnet test
```

---

## Running specific tests

```bash
# All tests
dotnet test

# Filter by category
dotnet test --filter "FullyQualifiedName~Api"
dotnet test --filter "FullyQualifiedName~Core"
dotnet test --filter "FullyQualifiedName~Infrastructure"
```

---

## Target framework

`.NET 9` — `net9.0`

---

## Continuous integration

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs on push or PR to `main`. Because `WebApplicationFactory<Program>` requires the compiled `Brusca.Api` entry-point assembly, the workflow checks out **both repos** as siblings before building:

```
workspace/
  Brusca.Tests/    ← this repo
  Brusca.Api/      ← checked out from Out-Of-Box-Thinking/Brusca.Api @ main
```

It then runs `dotnet restore` → `dotnet build` → `dotnet test` and uploads the TRX results as an artifact.

---

## Related repositories

| Repo | Role |
|------|------|
| [Brusca.Core](https://github.com/Out-Of-Box-Thinking/Brusca.Core) | Domain kernel — interfaces and models (NuGet) |
| [Brusca.Infrastructure](https://github.com/Out-Of-Box-Thinking/Brusca.Infrastructure) | Infrastructure implementations (NuGet) |
| [Brusca.Api](https://github.com/Out-Of-Box-Thinking/Brusca.Api) | ASP.NET Core 9 host (ProjectReference for Mvc.Testing) |
| [Brusca.Web](https://github.com/Out-Of-Box-Thinking/Brusca.Web) | Astro 5 front-end |


---

## PII redaction & structure planning (NEW)

Brusca now strips PII from every file BEFORE any data is sent to Claude, encrypts the original PII at rest, and applies a Claude-designed directory layout to either the source path or an alternate path \u2014 recording the before/after of every operation for full audit traceability.

### Pipeline at a glance

1. **Read** \u2014 a supported reader pulls file content into memory.
2. **Redact** \u2014 `IPiiRedactionService` replaces every PII span with a stable token (`[[PII:Kind:NNNN]]`).
3. **Classify** \u2014 `IDocumentTypeClassifier` assigns a `DocumentType` from the redacted text + extension.
4. **Encrypt** \u2014 `IEncryptionService` (ASP.NET Core Data Protection) seals the PII JSON into a database column.
5. **Plan** \u2014 `IClaudeStructureService` calls Claude with ONLY `DocumentType` + extension counts; receives a `DirectoryStructurePlan` of folder/file templates.
6. **Execute** \u2014 `IStructureExecutionService` decrypts the PII in memory, fills the templates, performs the move/rename/create, and records every before/after path into `cleaning.FileRelocation`.

### Three new database tables

- `cleaning.RedactedFile`  \u2014 redacted descriptor + encrypted PII column.
- `cleaning.StructurePlan` \u2014 Claude-generated layout.
- `cleaning.FileRelocation`\u2014 before/after audit log.

DDL is in `Brusca.Core/docs/DeveloperGuide.md`.

### Five new API endpoints

- `POST /api/cleanings/{id}/redact`
- `POST /api/cleanings/{id}/generate-structure`
- `GET  /api/cleanings/{id}/structure-plan`
- `POST /api/cleanings/{id}/execute-structure`
- `GET  /api/cleanings/{id}/relocations`

Full docs in `Brusca.Api/docs/UserGuide.md`.

---

## Documentation index

| Topic | Location |
|-------|----------|
| Step-by-step install | `docs/InstallGuide.md` |
| End-user / integrator guide | `docs/UserGuide.md` |
| Engineer / contributor guide | `docs/DeveloperGuide.md` |

For the cross-repo pipeline overview, start in `Brusca.Core/docs/UserGuide.md` then walk down through `Brusca.Infrastructure`, `Brusca.Api`, `Brusca.Web`, and `Brusca.Tests`.
