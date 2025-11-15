using FluentAssertions;
using Moq;
using OpenAIDictate.Models;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Integration;

/// <summary>
/// Integration tests for TranscriptionService
/// These tests require actual API keys and network connectivity
/// Mark with [Fact(Skip = "Requires API key")] to skip in CI
/// </summary>
public class TranscriptionServiceIntegrationTests
{
    [Fact(Skip = "Requires OpenAI API key and network")]
    public async Task TranscribeAsync_ValidAudioStream_ShouldReturnTranscription()
    {
        // Arrange
        var config = new AppConfig
        {
            Model = "gpt-4o-transcribe",
            Language = "en",
            EnablePostProcessing = false,
            EnableVAD = false
        };
        
        const string apiKey = "sk-test"; // Replace with actual key for testing
        var logger = new SerilogLogger();
        var metrics = new MetricsService(logger);
        var service = new TranscriptionService(config, apiKey, logger, metrics);
        
        // Create minimal valid WAV stream
        var audioStream = CreateMinimalValidWavStream();

        // Act
        var result = await service.TranscribeAsync(audioStream);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TranscribeAsync_EmptyStream_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new AppConfig();
        var logger = new Mock<IAppLogger>().Object;
        var metrics = new Mock<IMetricsService>().Object;
        var service = new TranscriptionService(config, "sk-test", logger, metrics);
        var emptyStream = new MemoryStream();

        // Act & Assert
        var act = async () => await service.TranscribeAsync(emptyStream);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static MemoryStream CreateMinimalValidWavStream()
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        // RIFF header
        writer.Write("RIFF".ToCharArray());
        writer.Write(36); // Chunk size
        writer.Write("WAVE".ToCharArray());

        // fmt chunk
        writer.Write("fmt ".ToCharArray());
        writer.Write(16); // Subchunk1Size
        writer.Write((short)1); // AudioFormat (PCM)
        writer.Write((short)1); // Channels (mono)
        writer.Write(16000); // SampleRate
        writer.Write(32000); // ByteRate
        writer.Write((short)2); // BlockAlign
        writer.Write((short)16); // BitsPerSample

        // data chunk
        writer.Write("data".ToCharArray());
        writer.Write(0); // Data size

        stream.Position = 0;
        return stream;
    }
}

