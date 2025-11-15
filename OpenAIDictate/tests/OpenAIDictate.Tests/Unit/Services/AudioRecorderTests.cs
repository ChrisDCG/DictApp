using FluentAssertions;
using Moq;
using OpenAIDictate.Models;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Unit tests for AudioRecorder
/// Note: These tests may require audio hardware or mocking NAudio
/// </summary>
public class AudioRecorderTests
{
    private readonly AppConfig _config;

    public AudioRecorderTests()
    {
        _config = new AppConfig
        {
            MaxRecordingMinutes = 10
        };
    }

    [Fact]
    public void Constructor_ValidConfig_ShouldInitialize()
    {
        // Act
        var recorder = new AudioRecorder(_config);

        // Assert
        recorder.Should().NotBeNull();
        recorder.IsRecording.Should().BeFalse();
    }

    [Fact]
    public void Constructor_NullConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new AudioRecorder(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HasExceededMaxDuration_NotRecording_ShouldReturnFalse()
    {
        // Arrange
        var recorder = new AudioRecorder(_config);

        // Act
        var result = recorder.HasExceededMaxDuration();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetRecordingDuration_NotRecording_ShouldReturnZero()
    {
        // Arrange
        var recorder = new AudioRecorder(_config);

        // Act
        var duration = recorder.GetRecordingDuration();

        // Assert
        duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var recorder = new AudioRecorder(_config);

        // Act & Assert
        var act = () => recorder.Dispose();
        act.Should().NotThrow();
    }
}

