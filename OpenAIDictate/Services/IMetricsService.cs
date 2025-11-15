namespace OpenAIDictate.Services;

/// <summary>
/// Performance metrics and telemetry service interface
/// </summary>
public interface IMetricsService
{
    void RecordTranscriptionDuration(TimeSpan duration);
    void RecordTranscriptionLatency(TimeSpan latency);
    void RecordAudioDuration(TimeSpan duration);
    void RecordError(string errorType);
    void IncrementCounter(string counterName);
    void RecordGauge(string gaugeName, double value);
}

