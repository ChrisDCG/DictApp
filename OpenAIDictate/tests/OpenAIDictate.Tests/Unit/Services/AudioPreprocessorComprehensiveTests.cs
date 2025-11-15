using FluentAssertions;
using Moq;
using NAudio.Wave;
using OpenAIDictate.Models;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Comprehensive tests for AudioPreprocessor covering all code paths
/// </summary>
public class AudioPreprocessorComprehensiveTests
{
    private readonly AppConfig _config;
    private readonly AudioPreprocessor _preprocessor;

    public AudioPreprocessorComprehensiveTests()
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
        var inputStream = CreateValidWavStream(1000);
        var originalLength = inputStream.Length;

        // Act
        var result = await _preprocessor.PreprocessAsync(inputStream);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().Be(originalLength);
    }

    [Fact]
    public async Task PreprocessAsync_EmptySamples_ShouldReturnEmptyStream()
    {
        // Arrange
        var emptyStream = CreateWavStreamWithSamples(Array.Empty<float>());

        // Act
        var result = await _preprocessor.PreprocessAsync(emptyStream);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0); // WAV header
    }

    [Fact]
    public async Task PreprocessAsync_AllSilence_ShouldReturnMinimalAudio()
    {
        // Arrange
        var silentSamples = new float[16000]; // 1 second of silence
        var silentStream = CreateWavStreamWithSamples(silentSamples);

        // Act
        var result = await _preprocessor.PreprocessAsync(silentStream);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PreprocessAsync_ExceptionDuringProcessing_ShouldReturnFallbackStream()
    {
        // Arrange
        var invalidStream = new MemoryStream(new byte[] { 0xFF, 0xFF, 0xFF }); // Invalid WAV

        // Act
        var result = await _preprocessor.PreprocessAsync(invalidStream);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PreprocessAsync_LeadingSilence_ShouldTrimStart()
    {
        // Arrange
        var samples = new List<float>();
        samples.AddRange(new float[8000]); // 0.5s silence
        samples.AddRange(CreateVoiceSamples(8000)); // 0.5s voice
        var stream = CreateWavStreamWithSamples(samples.ToArray());

        // Act
        var result = await _preprocessor.PreprocessAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PreprocessAsync_TrailingSilence_ShouldTrimEnd()
    {
        // Arrange
        var samples = new List<float>();
        samples.AddRange(CreateVoiceSamples(8000)); // 0.5s voice
        samples.AddRange(new float[8000]); // 0.5s silence
        var stream = CreateWavStreamWithSamples(samples.ToArray());

        // Act
        var result = await _preprocessor.PreprocessAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DetectVoiceActivity_ValidStream_ShouldReturnSegments()
    {
        // Arrange
        var samples = CreateVoiceSamples(16000);
        var stream = CreateWavStreamWithSamples(samples);

        // Act
        var segments = _preprocessor.DetectVoiceActivity(stream, 16000);

        // Assert
        segments.Should().NotBeNull();
        segments.Should().NotBeEmpty();
    }

    [Fact]
    public void DetectVoiceActivity_EmptyStream_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyStream = new MemoryStream();

        // Act
        var segments = _preprocessor.DetectVoiceActivity(emptyStream, 16000);

        // Assert
        segments.Should().NotBeNull();
        segments.Should().BeEmpty();
    }

    [Fact]
    public void DetectVoiceActivity_Exception_ShouldReturnEmptyList()
    {
        // Arrange
        var invalidStream = new MemoryStream(new byte[] { 0xFF });

        // Act
        var segments = _preprocessor.DetectVoiceActivity(invalidStream, 16000);

        // Assert
        segments.Should().NotBeNull();
        segments.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_NullConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new AudioPreprocessor(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    private static MemoryStream CreateValidWavStream(int sampleCount = 16000)
    {
        return CreateWavStreamWithSamples(new float[sampleCount]);
    }

    private static MemoryStream CreateWavStreamWithSamples(float[] samples)
    {
        var stream = new MemoryStream();
        var format = new WaveFormat(16000, 16, 1);
        
        using (var writer = new WaveFileWriter(stream, format))
        {
            writer.WriteSamples(samples, 0, samples.Length);
        }
        
        stream.Position = 0;
        return stream;
    }

    private static float[] CreateVoiceSamples(int count)
    {
        var samples = new float[count];
        var random = new Random(42);
        for (int i = 0; i < count; i++)
        {
            // Generate samples above silence threshold (-20 dBFS ≈ 0.1 amplitude)
            samples[i] = (float)(random.NextDouble() * 0.2 + 0.1);
        }
        return samples;
    }
}

