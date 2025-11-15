using FluentAssertions;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Comprehensive tests for TextInjector covering all code paths
/// </summary>
public class TextInjectorComprehensiveTests
{
    [Fact]
    public async Task InjectAsync_EmptyString_ShouldNotThrow()
    {
        // Act & Assert
        var act = async () => await TextInjector.InjectAsync("");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InjectAsync_NullString_ShouldNotThrow()
    {
        // Act & Assert
        var act = async () => await TextInjector.InjectAsync(null!);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InjectAsync_ValidText_ShouldNotThrow()
    {
        // Arrange
        const string testText = "Test transcription text";

        // Act & Assert
        var act = async () => await TextInjector.InjectAsync(testText);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InjectAsync_LongText_ShouldNotThrow()
    {
        // Arrange
        var longText = new string('A', 10000);

        // Act & Assert
        var act = async () => await TextInjector.InjectAsync(longText);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InjectAsync_TextWithSpecialCharacters_ShouldNotThrow()
    {
        // Arrange
        const string specialText = "Test §123 €100 50% & more!";

        // Act & Assert
        var act = async () => await TextInjector.InjectAsync(specialText);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InjectAsync_TextWithNewlines_ShouldNotThrow()
    {
        // Arrange
        const string multilineText = "Line 1\nLine 2\nLine 3";

        // Act & Assert
        var act = async () => await TextInjector.InjectAsync(multilineText);
        await act.Should().NotThrowAsync();
    }
}

