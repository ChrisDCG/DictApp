using FluentAssertions;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Unit tests for SerilogLogger
/// </summary>
public class SerilogLoggerTests : IDisposable
{
    private readonly SerilogLogger _logger;

    public SerilogLoggerTests()
    {
        _logger = new SerilogLogger();
    }

    [Fact]
    public void LogInfo_ValidMessage_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _logger.LogInfo("Test info message");
        act.Should().NotThrow();
    }

    [Fact]
    public void LogInfo_WithParameters_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _logger.LogInfo("Test message with {Param}", "value");
        act.Should().NotThrow();
    }

    [Fact]
    public void LogError_ValidMessage_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _logger.LogError("Test error message");
        act.Should().NotThrow();
    }

    [Fact]
    public void LogError_WithException_ShouldNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert
        var act = () => _logger.LogError("Test error", exception);
        act.Should().NotThrow();
    }

    [Fact]
    public void LogError_WithExceptionAndParameters_ShouldNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert
        var act = () => _logger.LogError("Test error {Param}", exception, "value");
        act.Should().NotThrow();
    }

    [Fact]
    public void LogError_WithoutException_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _logger.LogError("Test error message");
        act.Should().NotThrow();
    }

    [Fact]
    public void LogError_WithoutExceptionWithParameters_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _logger.LogError("Test error {Param}", null, "value");
        act.Should().NotThrow();
    }

    [Fact]
    public void LogWarning_ValidMessage_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _logger.LogWarning("Test warning message");
        act.Should().NotThrow();
    }

    [Fact]
    public void LogDebug_ValidMessage_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _logger.LogDebug("Test debug message");
        act.Should().NotThrow();
    }

    [Fact]
    public void LogDebug_WithParameters_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _logger.LogDebug("Test message with {Param}", "value");
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _logger?.Dispose();
    }
}

