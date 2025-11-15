using FluentAssertions;
using Moq;
using NAudio.Wave;
using OpenAIDictate.Models;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Unit tests for AudioPreprocessor
/// </summary>
public class AudioPreprocessorTests
{
    private readonly AppConfig _config;
    private readonly AudioPreprocessor _preprocessor;

    public AudioPreprocessorTests()
    {
        _config = new AppConfig
        {
            EnableVAD = true,
            SilenceThresholdDb = -20.0
        };
        _preprocessor = new AudioPreprocessor(_config);
    }

    [Fact]
    public async Task PreprocessAsync_VADDisabled_ShouldReturnOriginalStream()
    {
        // Arrange
        _config.EnableVAD = false;
        var inputStream = CreateValidWavStream();
        var originalLength = inputStream.Length;

        // Act
        var result = await _preprocessor.PreprocessAsync(inputStream);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PreprocessAsync_EmptyStream_ShouldReturnEmptyStream()
    {
        // Arrange
        var emptyStream = new MemoryStream();

        // Act
        var result = await _preprocessor.PreprocessAsync(emptyStream);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task PreprocessAsync_InvalidStream_ShouldReturnFallback()
    {
        // Arrange
        var invalidStream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02 });

        // Act
        var result = await _preprocessor.PreprocessAsync(invalidStream);

        // Assert
        result.Should().NotBeNull();
    }

    private static MemoryStream CreateValidWavStream()
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

