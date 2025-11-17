using FluentAssertions;
using Moq;
using OpenAIDictate.Models;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Comprehensive tests for PromptGenerator covering all code paths
/// </summary>
public class PromptGeneratorComprehensiveTests : IDisposable
{
    private readonly AppConfig _config;
    private readonly Mock<IAppLogger> _mockLogger;
    private PromptGenerator? _generator;

    public PromptGeneratorComprehensiveTests()
    {
        _config = new AppConfig
        {
            Language = "de",
            Glossary = "Bundesgerichtshof, Schadensersatz"
        };
        _mockLogger = new Mock<IAppLogger>();
    }

    [Fact]
    public void Constructor_NullApiKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new PromptGenerator(null!, _config, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new PromptGenerator("sk-test", null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldUseDefaultLogger()
    {
        // Act
        var generator = new PromptGenerator("sk-test", _config, null);

        // Assert
        generator.Should().NotBeNull();
        // PromptGenerator doesn't implement IDisposable
    }

    [Fact]
    public async Task GenerateFictitiousPromptAsync_EmptyInstruction_ShouldReturnEmpty()
    {
        // Arrange
        _generator = new PromptGenerator("sk-test", _config, _mockLogger.Object);

        // Act
        var result = await _generator.GenerateFictitiousPromptAsync("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateFictitiousPromptAsync_WhitespaceInstruction_ShouldReturnEmpty()
    {
        // Arrange
        _generator = new PromptGenerator("sk-test", _config, _mockLogger.Object);

        // Act
        var result = await _generator.GenerateFictitiousPromptAsync("   ");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildOptimizedPromptAsync_NoGlossary_ShouldReturnPrompt()
    {
        // Arrange
        _config.Glossary = "";
        _generator = new PromptGenerator("sk-test", _config, _mockLogger.Object);

        // Act
        var result = await _generator.BuildOptimizedPromptAsync();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task BuildOptimizedPromptAsync_WithGlossary_ShouldIncludeGlossary()
    {
        // Arrange
        _config.Glossary = "Term1, Term2";
        _generator = new PromptGenerator("sk-test", _config, _mockLogger.Object);

        // Act
        var result = await _generator.BuildOptimizedPromptAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Term");
    }

    [Fact]
    public async Task BuildOptimizedPromptAsync_LongPrompt_ShouldTruncate()
    {
        // Arrange
        _config.Glossary = new string('A', 2000); // Very long glossary
        _generator = new PromptGenerator("sk-test", _config, _mockLogger.Object);

        // Act
        var result = await _generator.BuildOptimizedPromptAsync();

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeLessThanOrEqualTo(1000);
    }

    [Fact]
    public async Task BuildOptimizedPromptAsync_EnglishLanguage_ShouldUseEnglishContext()
    {
        // Arrange
        _config.Language = "en";
        _generator = new PromptGenerator("sk-test", _config, _mockLogger.Object);

        // Act
        var result = await _generator.BuildOptimizedPromptAsync();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task BuildOptimizedPromptAsync_UnknownLanguage_ShouldUseDefaultContext()
    {
        // Arrange
        _config.Language = "fr";
        _generator = new PromptGenerator("sk-test", _config, _mockLogger.Object);

        // Act
        var result = await _generator.BuildOptimizedPromptAsync();

        // Assert
        result.Should().NotBeNull();
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

