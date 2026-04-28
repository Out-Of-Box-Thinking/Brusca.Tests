# User Guide — Brusca.Tests

What lives in this repo and what each test class covers.

---

## 1. Test catalogue

| Test class | Covers |
|------------|--------|
| `Infrastructure.RegexPiiRedactionServiceTests`        | Detection of every built-in PII kind, token stability, disabled detectors. |
| `Infrastructure.HeuristicDocumentTypeClassifierTests` | Extension-first mapping + keyword fallbacks for invoice/contract/resume/medical. |
| `Infrastructure.DataProtectionEncryptionServiceTests` | Encrypt → decrypt round-trip, empty-string short-circuit. |
| `Api.CleaningsControllerTests`                        | Integration test of the API surface via `WebApplicationFactory<Program>` (legacy + redaction endpoints). |

---

## 2. Running selectively

```powershell
# All tests:
dotnet test

# Only PII pipeline:
dotnet test --filter "FullyQualifiedName~Infrastructure"

# A single class:
dotnet test --filter "FullyQualifiedName~RegexPiiRedactionServiceTests"

# A single test:
dotnet test --filter "FullyQualifiedName~RegexPiiRedactionServiceTests.RedactAsync_TokenIsStableAndOrdered"
```

---

## 3. Adding a new test

1. Mirror the namespace of the production code (`Brusca.Tests.Infrastructure` for code in `Brusca.Infrastructure`, `Brusca.Tests.Api` for controllers).
2. Use `Xunit` (`Fact`, `Theory`, `InlineData`) and `FluentAssertions` is permitted but not required.
3. Mock external services with `Moq`. The PII pipeline mocks easily because every dependency is an interface.
4. Generate fake data with `Bogus` for non-PII fields. **Never** include real PII in test fixtures — make-up obviously-fake values like `123-45-6789`.

---

## 4. Privacy in tests

- Fake PII used in tests must be obviously synthetic.
- Tests must NOT touch the production database. Repository unit tests use mocks; integration tests use a Testcontainers SQL Server or your team's CI database.
- Tests must NOT call the real Claude API. `IClaudeStructureService` is mocked in any test that exercises the pipeline.
