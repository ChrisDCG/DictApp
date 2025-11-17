using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using OpenAIDictate.Models;

namespace OpenAIDictate.Services;

/// <summary>
/// Audio preprocessing service that leverages Silero VAD and OpenAI cookbook guidance.
/// </summary>
public class AudioPreprocessor
{
    private static readonly object VadSync = new();
    private static Lazy<Task<SileroVoiceActivityDetector>> Vad = CreateVadFactory();
    private static Task<SileroVoiceActivityDetector>? _vadTask;

    private readonly AppConfig _config;

    public AudioPreprocessor(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    private static Lazy<Task<SileroVoiceActivityDetector>> CreateVadFactory()
    {
        return new Lazy<Task<SileroVoiceActivityDetector>>(
            () =>
            {
                var task = SileroVoiceActivityDetector.CreateAsync();
                Volatile.Write(ref _vadTask, task);
                return task;
            },
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private static async Task<SileroVoiceActivityDetector> GetVadAsync()
    {
        Task<SileroVoiceActivityDetector> task = Vad.Value;
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch
        {
            lock (VadSync)
            {
                if (ReferenceEquals(task, Volatile.Read(ref _vadTask)))
                {
                    Volatile.Write(ref _vadTask, null);
                    Vad = CreateVadFactory();
                }
            }

            throw;
        }
    }

    public async Task<MemoryStream> PreprocessAsync(Stream inputStream)
    {
        if (!_config.EnableVAD)
        {
            return await CopyAsync(inputStream).ConfigureAwait(false);
        }

        try
        {
            inputStream.Position = 0;
            using var reader = new WaveFileReader(inputStream);
            var format = reader.WaveFormat;

            var samples = ReadAllSamples(reader);
            if (samples.Count == 0)
            {
                Logger.LogWarning("No audio samples detected during preprocessing.");
                return await CopyAsync(inputStream).ConfigureAwait(false);
            }

            var vadParameters = BuildParameters(format.SampleRate);
            var vad = await GetVadAsync().ConfigureAwait(false);
            IReadOnlyList<SileroVoiceActivityDetector.SpeechSegment> segments = vad.DetectSpeechSegments(samples.ToArray(), format.SampleRate, vadParameters);

            float[] processed = segments.Count > 0
                ? ExtractSegments(samples, segments)
                : FallbackTrim(samples, format.SampleRate);

            Normalize(processed);

            var output = new MemoryStream();
            using (var writer = new WaveFileWriter(output, format))
            {
                writer.WriteSamples(processed, 0, processed.Length);
                await writer.FlushAsync().ConfigureAwait(false);
            }

            output.Position = 0;

            Logger.LogInfo($"Preprocessing complete. Original samples: {samples.Count}, Processed samples: {processed.Length}");

            return output;
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Silero preprocessing failed: {ex.Message}. Returning original audio.");
            return await CopyAsync(inputStream).ConfigureAwait(false);
        }
    }

    private static List<float> ReadAllSamples(WaveFileReader reader)
    {
        var format = reader.WaveFormat;
        var provider = reader.ToSampleProvider();
        int estimatedSamples = (int)(reader.Length / (format.BitsPerSample / 8.0) / format.Channels);
        var samples = new List<float>(Math.Max(estimatedSamples, format.SampleRate * 5));
        var buffer = new float[format.SampleRate];

        int read;
        while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
        {
            samples.AddRange(buffer.AsSpan(0, read).ToArray());
        }

        return samples;
    }

    private SileroVoiceActivityDetector.VadParameters BuildParameters(int sampleRate)
    {
        int minSilenceSamples = (int)(sampleRate * (_config.VadMinSilenceDurationMs / 1000.0));
        int minSpeechSamples = (int)(sampleRate * (_config.VadMinSpeechDurationMs / 1000.0));
        int padSamples = (int)(sampleRate * (_config.VadSpeechPaddingMs / 1000.0));
        int maxSpeechSamples = (int)(Math.Min(_config.MaxRecordingMinutes * 60, 600) * sampleRate);

        return new SileroVoiceActivityDetector.VadParameters(
            SpeechThreshold: (float)_config.VadSpeechThreshold,
            MinSilenceSamples: Math.Max(minSilenceSamples, sampleRate / 100),
            MinSpeechSamples: Math.Max(minSpeechSamples, sampleRate / 20),
            SpeechPadSamples: Math.Max(padSamples, sampleRate / 200),
            MaxSpeechSamples: maxSpeechSamples);
    }

    private static float[] ExtractSegments(IReadOnlyList<float> samples, IReadOnlyList<SileroVoiceActivityDetector.SpeechSegment> segments)
    {
        var result = new List<float>(segments.Sum(s => s.End - s.Start));
        foreach (var segment in segments)
        {
            for (int i = segment.Start; i < segment.End && i < samples.Count; i++)
            {
                result.Add(samples[i]);
            }
        }

        return result.ToArray();
    }

    private float[] FallbackTrim(IReadOnlyList<float> samples, int sampleRate)
    {
        Logger.LogWarning("Silero VAD found no speech. Falling back to amplitude-based trimming.");

        var trimmed = TrimSilence(samples.ToArray(), sampleRate);
        if (trimmed.Length == 0)
        {
            return samples.Take(sampleRate).ToArray();
        }

        return trimmed;
    }

    private static void Normalize(float[] samples)
    {
        float max = samples.Select(Math.Abs).DefaultIfEmpty(0).Max();
        if (max < 1e-6f)
        {
            return;
        }

        float scale = 0.99f / max;
        if (scale >= 1f)
        {
            return;
        }

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] *= scale;
        }
    }

    private async Task<MemoryStream> CopyAsync(Stream source)
    {
        var output = new MemoryStream();
        source.Position = 0;
        await source.CopyToAsync(output).ConfigureAwait(false);
        output.Position = 0;
        return output;
    }

    private float[] TrimSilence(float[] samples, int sampleRate)
    {
        if (samples.Length == 0)
        {
            return samples;
        }

        double threshold = Math.Pow(10, _config.SilenceThresholdDb / 20.0);
        int chunkSize = Math.Max(1, (int)(sampleRate * 0.01));

        int startIndex = 0;
        for (int i = 0; i < samples.Length; i += chunkSize)
        {
            int end = Math.Min(i + chunkSize, samples.Length);
            if (CalculateRms(samples, i, end) > threshold)
            {
                startIndex = i;
                break;
            }
        }

        int endIndex = samples.Length;
        for (int i = samples.Length - chunkSize; i >= 0; i -= chunkSize)
        {
            int start = Math.Max(i, 0);
            if (CalculateRms(samples, start, Math.Min(i + chunkSize, samples.Length)) > threshold)
            {
                endIndex = i + chunkSize;
                break;
            }
        }

        if (startIndex >= endIndex)
        {
            return Array.Empty<float>();
        }

        var trimmed = new float[endIndex - startIndex];
        Array.Copy(samples, startIndex, trimmed, 0, trimmed.Length);
        return trimmed;
    }

    private static double CalculateRms(float[] samples, int start, int end)
    {
        end = Math.Min(end, samples.Length);
        int count = Math.Max(1, end - start);

        double sum = 0;
        for (int i = start; i < end; i++)
        {
            sum += samples[i] * samples[i];
        }

        return Math.Sqrt(sum / count);
    }
}
