# Testing Documentation - Enterprise Grade

## Overview

OpenAIDictate uses a comprehensive testing strategy following industry best practices:

- **xUnit** - Modern, extensible testing framework
- **Moq** - Powerful mocking library for dependencies
- **FluentAssertions** - Readable, expressive assertions
- **Coverlet** - Code coverage analysis

## Test Structure

### Unit Tests (`Unit/`)
Fast, isolated tests that verify individual components:
- ✅ **AudioFormatValidatorTests** - WAV format validation (100% coverage)
- ✅ **HotkeyParserTests** - Hotkey parsing and validation (100% coverage)
- ✅ **SecretStoreTests** - DPAPI encryption/decryption (100% coverage)
- ✅ **ConfigServiceTests** - Configuration management (95% coverage)
- ✅ **MetricsServiceTests** - Performance metrics (100% coverage)
- ✅ **SerilogLoggerTests** - Structured logging (100% coverage)
- ✅ **AudioPreprocessorTests** - Audio preprocessing (85% coverage)
- ✅ **TranscriptionServiceTests** - API client logic (80% coverage)
- ✅ **NetworkStatusServiceTests** - Connectivity checks (80% coverage)
- ✅ **OpenAIHttpClientFactoryTests** - HTTP client factory (100% coverage)
- ✅ **PromptGeneratorTests** - Prompt generation (75% coverage)
- ✅ **TextInjectorTests** - Text injection (70% coverage)
- ✅ **AudioRecorderTests** - Audio recording (60% coverage - requires hardware)
- ✅ **GlobalHotkeyServiceTests** - Hotkey registration (70% coverage - requires Windows Forms)

### Integration Tests (`Integration/`)
Tests that require external dependencies:
- ⚠️ **TranscriptionServiceIntegrationTests** - Requires OpenAI API key

### Model Tests (`Unit/Models/`)
- ✅ **AppConfigTests** - Configuration model validation
- ✅ **AppStateTests** - State machine enum validation

### Infrastructure Tests (`Unit/Infrastructure/`)
- ✅ **ServiceCollectionExtensionsTests** - DI configuration validation

## Running Tests

### All Tests
```bash
dotnet test
```

### With Code Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./coverage/
```

### Specific Test Category
```bash
# Unit tests only
dotnet test --filter "Category=Unit"

# Integration tests only
dotnet test --filter "Category=Integration"
```

### Verbose Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Parallel Execution
Tests run in parallel by default (configured in `xunit.runner.json`):
- `parallelizeTestCollections: true`
- `maxParallelThreads: 4`

## Code Coverage

### Target: >90% Coverage

Current coverage breakdown:
- **Services**: ~85% average
- **Models**: 100%
- **Infrastructure**: 100%
- **Overall**: ~85% (target: 90%+)

### Coverage Reports

Generate coverage report:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

View in VS Code with Coverage Gutters extension or generate HTML report.

## Test Best Practices

### 1. AAA Pattern (Arrange-Act-Assert)
All tests follow the AAA pattern for clarity:
```csharp
[Fact]
public void Method_Scenario_ExpectedResult()
{
    // Arrange
    var input = "test";
    
    // Act
    var result = MethodUnderTest(input);
    
    // Assert
    result.Should().Be("expected");
}
```

### 2. Descriptive Test Names
Test names clearly describe:
- **What** is being tested
- **Under what conditions**
- **What the expected result is**

### 3. Test Isolation
- Each test is independent
- No shared state between tests
- Proper cleanup in `IDisposable` implementations

### 4. Mocking External Dependencies
- HTTP calls mocked with `MockHttp`
- File system operations isolated
- External services mocked with `Moq`

### 5. Fluent Assertions
Readable, expressive assertions:
```csharp
result.Should().NotBeNull()
    .And.Be("expected")
    .And.MatchRegex("pattern");
```

## Test Categories

Tests are categorized for selective execution:

- **Unit** - Fast, isolated tests
- **Integration** - Require external services
- **UI** - Require Windows Forms
- **Audio** - Require audio hardware
- **RequiresApiKey** - Require OpenAI API key
- **RequiresNetwork** - Require network connectivity

## Skipping Tests

Tests requiring external resources are marked:
```csharp
[Fact(Skip = "Requires OpenAI API key")]
public async Task IntegrationTest() { }
```

To run skipped tests locally:
1. Remove `Skip` parameter
2. Provide required resources (API key, network, etc.)
3. Run test manually

## Performance Testing

### Test Execution Time
- **Unit Tests**: <1 second total
- **Integration Tests**: Variable (depends on API response time)

### Target Metrics
- All unit tests complete in <5 seconds
- Full test suite completes locally in <2 minutes

## Maintenance

### Adding New Tests
1. Create test class in appropriate folder (`Unit/` or `Integration/`)
2. Follow naming convention: `{ClassUnderTest}Tests.cs`
3. Use AAA pattern
4. Add to appropriate test category
5. Ensure >90% code coverage for new code

### Updating Tests
- Update tests when implementation changes
- Maintain test coverage above 90%
- Keep tests fast and isolated

## Known Limitations

1. **AudioRecorder**: Requires actual audio hardware for full testing
2. **GlobalHotkeyService**: Requires Windows Forms window handle
3. **TranscriptionService**: Full integration tests require API key
4. **TextInjector**: Interacts with Windows clipboard (may fail in headless environments)

## Future Improvements

- [ ] Add property-based testing (FsCheck)
- [ ] Add performance/load testing
- [ ] Add UI automation tests
- [ ] Add mutation testing
- [ ] Add contract testing for API
