using System.IO;
using System.Media;

namespace OpenAIDictate.Services;

/// <summary>
/// Provides subtle audio cues for recording state changes.
/// </summary>
public static class AudioCueService
{
    private static readonly SoundPlayer StartCuePlayer;
    private static readonly SoundPlayer StopCuePlayer;
    private static readonly object SyncRoot = new();

    static AudioCueService()
    {
        StartCuePlayer = CreateTonePlayer(920, 70);
        StopCuePlayer = CreateTonePlayer(540, 90);
    }

    public static void PlayStartCue() => Play(StartCuePlayer);

    public static void PlayStopCue() => Play(StopCuePlayer);

    private static void Play(SoundPlayer player)
    {
        lock (SyncRoot)
        {
            try
            {
                player.Play();
            }
            catch
            {
                // Non critical - ignore audio errors
            }
        }
    }

    private static SoundPlayer CreateTonePlayer(int frequency, int durationMs)
    {
        byte[] waveData = GenerateSineWave(frequency, durationMs);
        var stream = new MemoryStream(waveData);
        return new SoundPlayer(stream);
    }

    private static byte[] GenerateSineWave(int frequency, int durationMs)
    {
        const int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * (durationMs / 1000.0));
        var buffer = new byte[44 + sampleCount * 2];

        // RIFF header
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, buffer, 0, 4);
        BitConverter.GetBytes(buffer.Length - 8).CopyTo(buffer, 4);
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "), 0, buffer, 8, 8);
        BitConverter.GetBytes(16).CopyTo(buffer, 16); // Subchunk1 size
        BitConverter.GetBytes((ushort)1).CopyTo(buffer, 20); // PCM
        BitConverter.GetBytes((ushort)1).CopyTo(buffer, 22); // Mono
        BitConverter.GetBytes(sampleRate).CopyTo(buffer, 24);
        BitConverter.GetBytes(sampleRate * 2).CopyTo(buffer, 28);
        BitConverter.GetBytes((ushort)2).CopyTo(buffer, 32);
        BitConverter.GetBytes((ushort)16).CopyTo(buffer, 34);
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("data"), 0, buffer, 36, 4);
        BitConverter.GetBytes(sampleCount * 2).CopyTo(buffer, 40);

        int dataOffset = 44;
        for (int i = 0; i < sampleCount; i++)
        {
            double t = (double)i / sampleRate;
            short amplitude = (short)(Math.Sin(2 * Math.PI * frequency * t) * short.MaxValue * 0.3);
            BitConverter.GetBytes(amplitude).CopyTo(buffer, dataOffset + i * 2);
        }

        return buffer;
    }
}
