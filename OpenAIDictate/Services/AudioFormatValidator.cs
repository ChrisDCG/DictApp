using System.Text;

namespace OpenAIDictate.Services;

/// <summary>
/// Validates WAV audio streams to ensure they match the optimal format for OpenAI transcription.
/// </summary>
public static class AudioFormatValidator
{
    public const int ExpectedSampleRate = 16000;
    public const int ExpectedBitsPerSample = 16;
    public const int ExpectedChannels = 1;

    /// <summary>
    /// Validates that a WAV stream is PCM, 16-bit, mono, and sampled at 16kHz.
    /// Throws an <see cref="InvalidOperationException"/> when the stream does not match requirements.
    /// </summary>
    public static void Validate(Stream audioStream)
    {
        if (audioStream == null)
            throw new ArgumentNullException(nameof(audioStream));

        if (!audioStream.CanSeek)
            throw new InvalidOperationException("Audio stream must support seeking for validation.");

        if (audioStream.Length < 44)
            throw new InvalidOperationException("Audio stream is too short to contain a valid WAV header.");

        long originalPosition = audioStream.Position;

        var errors = new List<string>();

        try
        {
            audioStream.Position = 0;

            using var reader = new BinaryReader(audioStream, Encoding.ASCII, leaveOpen: true);

            string chunkId = new(reader.ReadChars(4));
            reader.ReadInt32(); // chunk size (unused)
            string format = new(reader.ReadChars(4));

            if (!chunkId.Equals("RIFF", StringComparison.Ordinal))
            {
                errors.Add("Ungültiger WAV-Header (RIFF fehlt).");
            }

            if (!format.Equals("WAVE", StringComparison.Ordinal))
            {
                errors.Add("Audio ist nicht im WAVE-Format.");
            }

            string subChunk1Id = new(reader.ReadChars(4));
            int subChunk1Size = reader.ReadInt32();

            if (!subChunk1Id.Equals("fmt ", StringComparison.Ordinal))
            {
                errors.Add("WAV fmt-Chunk nicht gefunden.");
            }

            short audioFormat = reader.ReadInt16();
            short channels = reader.ReadInt16();
            int sampleRate = reader.ReadInt32();
            reader.ReadInt32(); // byte rate (unused)
            short blockAlign = reader.ReadInt16();
            short bitsPerSample = reader.ReadInt16();

            if (subChunk1Size > 16)
            {
                reader.ReadBytes(subChunk1Size - 16); // skip any extra fmt bytes
            }

            if (audioFormat != 1)
            {
                errors.Add("Audioformat muss PCM sein (16-Bit).");
            }

            if (channels != ExpectedChannels)
            {
                errors.Add($"Es werden {ExpectedChannels} Kanal erwartet, gefunden: {channels}.");
            }

            if (sampleRate != ExpectedSampleRate)
            {
                errors.Add($"Sample-Rate muss {ExpectedSampleRate} Hz sein, gefunden: {sampleRate} Hz.");
            }

            if (bitsPerSample != ExpectedBitsPerSample)
            {
                errors.Add($"Bit-Tiefe muss {ExpectedBitsPerSample}-Bit sein, gefunden: {bitsPerSample}-Bit.");
            }

            if (blockAlign != channels * (bitsPerSample / 8))
            {
                errors.Add("BlockAlign stimmt nicht mit der Kanal-/Bit-Konfiguration überein.");
            }
        }
        catch (EndOfStreamException)
        {
            errors.Add("Audio-Stream ist beschädigt oder abgeschnitten.");
        }
        finally
        {
            if (audioStream.CanSeek)
            {
                audioStream.Position = originalPosition;
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Ungültiges Audioformat: " + string.Join(" ", errors));
        }
    }
}
