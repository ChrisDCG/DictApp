using FluentAssertions;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Unit tests for TextInjector
/// Note: These tests interact with Windows clipboard and may require UI thread
/// </summary>
public class TextInjectorTests
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
}
