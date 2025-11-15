using FluentAssertions;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Unit tests for AudioFormatValidator
/// </summary>
public class AudioFormatValidatorTests
{
    [Fact]
    public void Validate_ValidWavStream_ShouldNotThrow()
    {
        // Arrange
        var validWavStream = CreateValidWavStream();

        // Act & Assert
        var act = () => AudioFormatValidator.Validate(validWavStream);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_NullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => AudioFormatValidator.Validate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_TooShortStream_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var shortStream = new MemoryStream(new byte[10]);

        // Act & Assert
        var act = () => AudioFormatValidator.Validate(shortStream);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*too short*");
    }

    [Fact]
    public void Validate_InvalidSampleRate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidStream = CreateWavStreamWithSampleRate(44100); // Wrong sample rate

        // Act & Assert
        var act = () => AudioFormatValidator.Validate(invalidStream);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Sample-Rate*");
    }

    [Fact]
    public void Validate_InvalidChannels_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidStream = CreateWavStreamWithChannels(2); // Stereo instead of mono

        // Act & Assert
        var act = () => AudioFormatValidator.Validate(invalidStream);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Kanal*");
    }

    [Fact]
    public void Validate_InvalidBitsPerSample_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidStream = CreateWavStreamWithBitsPerSample(24); // Wrong bit depth

        // Act & Assert
        var act = () => AudioFormatValidator.Validate(invalidStream);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Bit-Tiefe*");
    }

    private static MemoryStream CreateValidWavStream()
    {
        return CreateWavStream(16000, 1, 16);
    }

    private static MemoryStream CreateWavStreamWithSampleRate(int sampleRate)
    {
        return CreateWavStream(sampleRate, 1, 16);
    }

    private static MemoryStream CreateWavStreamWithChannels(int channels)
    {
        return CreateWavStream(16000, channels, 16);
    }

    private static MemoryStream CreateWavStreamWithBitsPerSample(int bitsPerSample)
    {
        return CreateWavStream(16000, 1, bitsPerSample);
    }

    private static MemoryStream CreateWavStream(int sampleRate, int channels, int bitsPerSample)
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
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * (bitsPerSample / 8)); // ByteRate
        writer.Write((short)(channels * (bitsPerSample / 8))); // BlockAlign
        writer.Write((short)bitsPerSample);

        // data chunk
        writer.Write("data".ToCharArray());
        writer.Write(0); // Data size

        stream.Position = 0;
        return stream;
    }
}

