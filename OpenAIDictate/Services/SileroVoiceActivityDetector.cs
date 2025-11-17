using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenAIDictate.Infrastructure;

namespace OpenAIDictate.Services;

/// <summary>
/// High-fidelity Silero VAD integration based on the official ONNX export.
/// Provides speech probability estimates for 16 kHz mono audio.
/// </summary>
public sealed class SileroVoiceActivityDetector : IDisposable
{
    private const int SampleRate16k = 16000;
    private const int SampleRate8k = 8000;
    private const int HiddenStateSize = 128;

    private readonly InferenceSession _session;
    private readonly object _lock = new();

    private float[] _state = new float[2 * HiddenStateSize];
    private float[] _context = Array.Empty<float>();
    private int _lastSampleRate;
    private bool _disposed;

    private SileroVoiceActivityDetector(InferenceSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        ResetStates();
    }

    public static async Task<SileroVoiceActivityDetector> CreateAsync(
        string? modelPath = null,
        CancellationToken cancellationToken = default)
    {
        string resolvedModelPath = await ModelAssetManager
            .EnsureSileroVadModelAsync(modelPath, cancellationToken)
            .ConfigureAwait(false);

        var sessionOptions = new SessionOptions
        {
            InterOpNumThreads = 1,
            IntraOpNumThreads = 1
        };

        var session = new InferenceSession(resolvedModelPath, sessionOptions);
        Logger.LogInfo($"Silero VAD model ready: {resolvedModelPath}");
        return new SileroVoiceActivityDetector(session);
    }

    /// <summary>
    /// Computes speech probabilities for the provided audio and returns trimmed speech segments.
    /// </summary>
    public IReadOnlyList<SpeechSegment> DetectSpeechSegments(float[] audio, int sampleRate, VadParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(audio);
        if (audio.Length == 0)
        {
            return Array.Empty<SpeechSegment>();
        }

        if (sampleRate != SampleRate16k && sampleRate != SampleRate8k)
        {
            throw new NotSupportedException($"Silero VAD supports 8 kHz or 16 kHz audio. Received {sampleRate} Hz.");
        }

        int chunkSize = sampleRate == SampleRate16k ? 512 : 256;
        var chunkBuffer = ArrayPool<float>.Shared.Rent(chunkSize);
        try
        {
            var probabilities = new List<float>((audio.Length / chunkSize) + 2);

            ResetStates();
            lock (_lock)
            {
                EnsureState(sampleRate, batchSize: 1);

                for (int offset = 0; offset < audio.Length; offset += chunkSize)
                {
                    Array.Clear(chunkBuffer, 0, chunkSize);
                    int copy = Math.Min(chunkSize, audio.Length - offset);
                    Array.Copy(audio, offset, chunkBuffer, 0, copy);

                    float probability = Forward(chunkBuffer, sampleRate);
                    probabilities.Add(probability);
                }
            }

            return PostProcess(probabilities, audio.Length, sampleRate, parameters, chunkSize);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(chunkBuffer);
        }
    }

    public void ResetStates()
    {
        lock (_lock)
        {
            _state = new float[2 * HiddenStateSize];
            _context = Array.Empty<float>();
            _lastSampleRate = 0;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.Dispose();
        _disposed = true;
    }

    private void EnsureState(int sampleRate, int batchSize)
    {
        if (_lastSampleRate != sampleRate || _context.Length == 0)
        {
            _state = new float[2 * batchSize * HiddenStateSize];
            int contextSize = sampleRate == SampleRate16k ? 64 : 32;
            _context = new float[contextSize];
            _lastSampleRate = sampleRate;
        }
    }

    private float Forward(ReadOnlySpan<float> chunk, int sampleRate)
    {
        int chunkSize = sampleRate == SampleRate16k ? 512 : 256;
        if (chunk.Length != chunkSize)
        {
            throw new ArgumentException($"Chunk must contain exactly {chunkSize} samples.", nameof(chunk));
        }

        int contextSize = _context.Length;
        var inputLength = contextSize + chunkSize;
        var inputBuffer = ArrayPool<float>.Shared.Rent(inputLength);
        try
        {
            Array.Copy(_context, inputBuffer, contextSize);
            chunk.CopyTo(inputBuffer.AsSpan(contextSize));

            var inputTensor = new DenseTensor<float>(inputBuffer, new[] { 1, inputLength });
            var stateTensor = new DenseTensor<float>(_state, new[] { 2, 1, HiddenStateSize });
            var srTensor = new DenseTensor<long>(new[] { (long)sampleRate }, new[] { 1 });

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor),
                NamedOnnxValue.CreateFromTensor("state", stateTensor),
                NamedOnnxValue.CreateFromTensor("sr", srTensor)
            };

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs);

            var probabilityTensor = results[0].AsTensor<float>();
            float probability = probabilityTensor[0];

            var stateOutputTensor = results[1].AsTensor<float>();
            var stateOutputDense = stateOutputTensor.ToDenseTensor();
            stateOutputDense.Buffer.Span.CopyTo(_state);

            Array.Copy(inputBuffer, inputLength - contextSize, _context, 0, contextSize);

            return probability;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(inputBuffer);
        }
    }

    private static IReadOnlyList<SpeechSegment> PostProcess(
        IReadOnlyList<float> probabilities,
        int totalSamples,
        int sampleRate,
        VadParameters parameters,
        int chunkSize)
    {
        var segments = new List<SpeechSegment>();

        bool triggered = false;
        int speechStart = 0;
        int silenceStart = -1;
        int maxSpeechSamples = parameters.MaxSpeechSamples > 0 ? parameters.MaxSpeechSamples : int.MaxValue;
        float negThreshold = Math.Max(parameters.SpeechThreshold - 0.15f, 0.01f);

        for (int i = 0; i < probabilities.Count; i++)
        {
            int chunkStart = i * chunkSize;
            float probability = probabilities[i];

            if (!triggered)
            {
                if (probability >= parameters.SpeechThreshold)
                {
                    triggered = true;
                    speechStart = chunkStart;
                    silenceStart = -1;
                }

                continue;
            }

            if (probability >= parameters.SpeechThreshold)
            {
                silenceStart = -1;
            }
            else if (probability < negThreshold)
            {
                if (silenceStart < 0)
                {
                    silenceStart = chunkStart;
                }

                if (chunkStart + chunkSize - silenceStart >= parameters.MinSilenceSamples)
                {
                    int endSample = silenceStart;
                    if (endSample - speechStart >= parameters.MinSpeechSamples)
                    {
                        segments.Add(new SpeechSegment(speechStart, endSample));
                    }

                    triggered = false;
                    silenceStart = -1;
                    continue;
                }
            }

            if (chunkStart + chunkSize - speechStart >= maxSpeechSamples)
            {
                int endSample = silenceStart > 0 ? silenceStart : chunkStart + chunkSize;
                if (endSample - speechStart >= parameters.MinSpeechSamples)
                {
                    segments.Add(new SpeechSegment(speechStart, endSample));
                }

                triggered = false;
                silenceStart = -1;
            }
        }

        if (triggered)
        {
            int endSample = totalSamples;
            if (endSample - speechStart >= parameters.MinSpeechSamples)
            {
                segments.Add(new SpeechSegment(speechStart, endSample));
            }
        }

        if (segments.Count == 0)
        {
            return segments;
        }

        // Apply padding and merge touching segments
        var merged = new List<SpeechSegment>();
        foreach (var segment in segments.OrderBy(s => s.Start))
        {
            int paddedStart = Math.Max(0, segment.Start - parameters.SpeechPadSamples);
            int paddedEnd = Math.Min(totalSamples, segment.End + parameters.SpeechPadSamples);

            if (merged.Count > 0 && paddedStart <= merged[^1].End)
            {
                var last = merged[^1];
                merged[^1] = new SpeechSegment(last.Start, Math.Max(last.End, paddedEnd));
            }
            else
            {
                merged.Add(new SpeechSegment(paddedStart, paddedEnd));
            }
        }

        return merged;
    }

    public readonly record struct SpeechSegment(int Start, int End);

    public readonly record struct VadParameters(
        float SpeechThreshold,
        int MinSilenceSamples,
        int MinSpeechSamples,
        int SpeechPadSamples,
        int MaxSpeechSamples);
}
