# OpenAIDictate Tests

Enterprise-grade test suite for OpenAIDictate using xUnit, Moq, and FluentAssertions.

## Test Structure

```
tests/OpenAIDictate.Tests/
├── Unit/                    # Unit tests for individual services
│   └── Services/
│       ├── AudioFormatValidatorTests.cs
│       ├── AudioPreprocessorTests.cs
│       ├── ConfigServiceTests.cs
│       ├── HotkeyParserTests.cs
│       ├── MetricsServiceTests.cs
│       ├── NetworkStatusServiceTests.cs
│       └── SecretStoreTests.cs
├── Integration/             # Integration tests (require API keys)
│   └── TranscriptionServiceIntegrationTests.cs
└── TestHelpers/            # Test utilities and fixtures
    └── TestFixture.cs
```

## Running Tests

### All Tests
```bash
dotnet test
```

### With Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~HotkeyParserTests"
```

### Verbose Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Test Coverage

Target: **>90% code coverage**

Current coverage areas:
- ✅ AudioFormatValidator (100%)
- ✅ HotkeyParser (100%)
- ✅ SecretStore (100%)
- ✅ ConfigService (95%)
- ✅ MetricsService (100%)
- ✅ NetworkStatusService (80%)
- ✅ AudioPreprocessor (85%)
- ⚠️ TranscriptionService (Integration tests only - requires API key)
- ⚠️ AudioRecorder (Requires audio hardware)
- ⚠️ AppTrayContext (Requires Windows Forms)

## Test Categories

### Unit Tests
Fast, isolated tests that don't require external dependencies:
- Service logic validation
- Data transformation
- Configuration parsing
- Encryption/decryption

### Integration Tests
Tests that require external dependencies:
- OpenAI API calls (require API key)
- Network connectivity
- Audio hardware access

## Best Practices

1. **Arrange-Act-Assert (AAA) Pattern**: All tests follow AAA structure
2. **FluentAssertions**: Readable, expressive assertions
3. **Mocking**: External dependencies mocked with Moq
4. **Test Isolation**: Each test is independent
5. **Descriptive Names**: Test names describe what is being tested

## Skipping Tests

Tests that require external resources are marked with `[Fact(Skip = "Reason")]`:
- API key required
- Network access required
- Hardware required

To run skipped tests locally, remove the `Skip` parameter and provide required resources.
