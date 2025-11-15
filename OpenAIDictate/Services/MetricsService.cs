using System.Collections.Concurrent;

namespace OpenAIDictate.Services;

/// <summary>
/// In-memory metrics service for performance monitoring
/// Tracks transcription performance, errors, and system metrics
/// </summary>
public sealed class MetricsService : IMetricsService
{
    private readonly IAppLogger _logger;
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, double> _gauges = new();
    private readonly ConcurrentBag<TimeSpan> _transcriptionDurations = new();
    private readonly ConcurrentBag<TimeSpan> _transcriptionLatencies = new();
    private readonly ConcurrentBag<TimeSpan> _audioDurations = new();
    private readonly ConcurrentBag<string> _errors = new();

    public MetricsService(IAppLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RecordTranscriptionDuration(TimeSpan duration)
    {
        _transcriptionDurations.Add(duration);
        _logger.LogDebug("Transcription duration: {Duration}ms", duration.TotalMilliseconds);
    }

    public void RecordTranscriptionLatency(TimeSpan latency)
    {
        _transcriptionLatencies.Add(latency);
        _logger.LogDebug("Transcription latency: {Latency}ms", latency.TotalMilliseconds);
    }

    public void RecordAudioDuration(TimeSpan duration)
    {
        _audioDurations.Add(duration);
    }

    public void RecordError(string errorType)
    {
        _errors.Add(errorType);
        IncrementCounter($"error.{errorType}");
        _logger.LogWarning("Error recorded: {ErrorType}", errorType);
    }

    public void IncrementCounter(string counterName)
    {
        _counters.AddOrUpdate(counterName, 1, (key, value) => value + 1);
    }

    public void RecordGauge(string gaugeName, double value)
    {
        _gauges.AddOrUpdate(gaugeName, value, (key, oldValue) => value);
    }

    /// <summary>
    /// Gets current metrics snapshot
    /// </summary>
    public MetricsSnapshot GetSnapshot()
    {
        return new MetricsSnapshot
        {
            TranscriptionCount = _transcriptionDurations.Count,
            AverageTranscriptionDuration = _transcriptionDurations.Count > 0
                ? TimeSpan.FromMilliseconds(_transcriptionDurations.Average(d => d.TotalMilliseconds))
                : TimeSpan.Zero,
            AverageLatency = _transcriptionLatencies.Count > 0
                ? TimeSpan.FromMilliseconds(_transcriptionLatencies.Average(l => l.TotalMilliseconds))
                : TimeSpan.Zero,
            TotalAudioDuration = TimeSpan.FromSeconds(_audioDurations.Sum(d => d.TotalSeconds)),
            ErrorCount = _errors.Count,
            Counters = new Dictionary<string, long>(_counters),
            Gauges = new Dictionary<string, double>(_gauges)
        };
    }

    /// <summary>
    /// Resets all metrics
    /// </summary>
    public void Reset()
    {
        _transcriptionDurations.Clear();
        _transcriptionLatencies.Clear();
        _audioDurations.Clear();
        _errors.Clear();
        _counters.Clear();
        _gauges.Clear();
    }
}

/// <summary>
/// Metrics snapshot for reporting
/// </summary>
public sealed class MetricsSnapshot
{
    public int TranscriptionCount { get; set; }
    public TimeSpan AverageTranscriptionDuration { get; set; }
    public TimeSpan AverageLatency { get; set; }
    public TimeSpan TotalAudioDuration { get; set; }
    public int ErrorCount { get; set; }
    public Dictionary<string, long> Counters { get; set; } = new();
    public Dictionary<string, double> Gauges { get; set; } = new();
}

