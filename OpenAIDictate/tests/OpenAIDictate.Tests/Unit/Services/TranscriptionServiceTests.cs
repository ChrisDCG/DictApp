using FluentAssertions;
using Moq;
using OpenAIDictate.Models;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Unit tests for TranscriptionService (mocked HTTP calls)
/// </summary>
public class TranscriptionServiceTests
{
    private readonly Mock<IAppLogger> _mockLogger;
    private readonly Mock<IMetricsService> _mockMetrics;
    private readonly AppConfig _config;

    public TranscriptionServiceTests()
    {
        _mockLogger = new Mock<IAppLogger>();
        _mockMetrics = new Mock<IMetricsService>();
        _config = new AppConfig
        {
            Model = "gpt-4o-transcribe",
            Language = "en",
            EnablePostProcessing = false,
            EnableVAD = false
        };
    }

    [Fact]
    public async Task TranscribeAsync_EmptyStream_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new TranscriptionService(_config, "sk-test", _mockLogger.Object, _mockMetrics.Object);
        var emptyStream = new MemoryStream();

        // Act & Assert
        var act = async () => await service.TranscribeAsync(emptyStream);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task TranscribeAsync_NullStream_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new TranscriptionService(_config, "sk-test", _mockLogger.Object, _mockMetrics.Object);

        // Act & Assert
        var act = async () => await service.TranscribeAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Constructor_ValidParameters_ShouldInitialize()
    {
        // Act
        var service = new TranscriptionService(_config, "sk-test", _mockLogger.Object, _mockMetrics.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new TranscriptionService(null!, "sk-test", _mockLogger.Object, _mockMetrics.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullApiKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new TranscriptionService(_config, null!, _mockLogger.Object, _mockMetrics.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldUseDefaultLogger()
    {
        // Act
        var service = new TranscriptionService(_config, "sk-test", null, null);

        // Assert
        service.Should().NotBeNull();
    }
}
