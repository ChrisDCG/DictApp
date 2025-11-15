using FluentAssertions;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Unit tests for static Logger class
/// </summary>
public class LoggerTests
{
    [Fact]
    public void LogInfo_ValidMessage_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => Logger.LogInfo("Test info message");
        act.Should().NotThrow();
    }

    [Fact]
    public void LogWarning_ValidMessage_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => Logger.LogWarning("Test warning message");
        act.Should().NotThrow();
    }

    [Fact]
    public void LogError_ValidMessage_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => Logger.LogError("Test error message");
        act.Should().NotThrow();
    }

    [Fact]
    public void LogError_WithException_ShouldNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert
        var act = () => Logger.LogError("Test error", exception);
        act.Should().NotThrow();
    }
}

