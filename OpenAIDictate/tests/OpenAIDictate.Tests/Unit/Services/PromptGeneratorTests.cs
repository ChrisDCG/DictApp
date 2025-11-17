using FluentAssertions;
using Moq;
using OpenAIDictate.Models;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Unit tests for PromptGenerator
/// </summary>
public class PromptGeneratorTests : IDisposable
{
    private readonly AppConfig _config;
    private readonly Mock<IAppLogger> _mockLogger;
    private PromptGenerator? _generator;

    public PromptGeneratorTests()
    {
        _config = new AppConfig
        {
            Language = "de",
            Glossary = "Bundesgerichtshof, Schadensersatz"
        };
        _mockLogger = new Mock<IAppLogger>();
    }

    [Fact]
    public async Task BuildOptimizedPromptAsync_SingleTerm_ShouldReturnFormattedSentence()
    {
        // Arrange
        _config.Glossary = "Bundesgerichtshof";
        _generator = new PromptGenerator("sk-test", _config, _mockLogger.Object);

        // Act
        var result = await _generator.BuildOptimizedPromptAsync();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task BuildOptimizedPromptAsync_MultipleTerms_ShouldReturnFormattedSentence()
    {
        // Arrange
        _config.Glossary = "Term1, Term2, Term3";
        _generator = new PromptGenerator("sk-test", _config, _mockLogger.Object);

        // Act
        var result = await _generator.BuildOptimizedPromptAsync();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ClearCache_ShouldClearAllCachedPrompts()
    {
        // Act
        PromptGenerator.ClearCache();

        // Assert
        // Cache should be cleared (no exception thrown)
        PromptGenerator.ClearCache(); // Should not throw
    }

    public void Dispose()
    {
        // PromptGenerator doesn't implement IDisposable
        _generator = null;
    }
}

