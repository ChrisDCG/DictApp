using FluentAssertions;
using Moq;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Comprehensive tests for MetricsService covering all code paths
/// </summary>
public class MetricsServiceComprehensiveTests
{
    private readonly Mock<IAppLogger> _mockLogger;
    private readonly MetricsService _metricsService;

    public MetricsServiceComprehensiveTests()
    {
        _mockLogger = new Mock<IAppLogger>();
        _metricsService = new MetricsService(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new MetricsService(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordTranscriptionDuration_ShouldRecordDuration()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5.5);

        // Act
        _metricsService.RecordTranscriptionDuration(duration);

        // Assert
        var snapshot = _metricsService.GetSnapshot();
        snapshot.TranscriptionCount.Should().Be(1);
        snapshot.AverageTranscriptionDuration.Should().BeCloseTo(duration, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void RecordTranscriptionLatency_ShouldRecordLatency()
    {
        // Arrange
        var latency = TimeSpan.FromMilliseconds(250);

        // Act
        _metricsService.RecordTranscriptionLatency(latency);

        // Assert
        var snapshot = _metricsService.GetSnapshot();
        snapshot.AverageLatency.Should().BeCloseTo(latency, TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public void RecordAudioDuration_ShouldRecordDuration()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(2);

        // Act
        _metricsService.RecordAudioDuration(duration);

        // Assert
        var snapshot = _metricsService.GetSnapshot();
        snapshot.TotalAudioDuration.Should().BeCloseTo(duration, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void RecordError_ShouldIncrementErrorCount()
    {
        // Arrange
        const string errorType = "network_error";

        // Act
        _metricsService.RecordError(errorType);

        // Assert
        var snapshot = _metricsService.GetSnapshot();
        snapshot.ErrorCount.Should().Be(1);
        snapshot.Counters.Should().ContainKey($"error.{errorType}");
        snapshot.Counters[$"error.{errorType}"].Should().Be(1);
    }

    [Fact]
    public void IncrementCounter_ShouldIncrementCounter()
    {
        // Arrange
        const string counterName = "test_counter";

        // Act
        _metricsService.IncrementCounter(counterName);
        _metricsService.IncrementCounter(counterName);

        // Assert
        var snapshot = _metricsService.GetSnapshot();
        snapshot.Counters.Should().ContainKey(counterName);
        snapshot.Counters[counterName].Should().Be(2);
    }

    [Fact]
    public void RecordGauge_ShouldSetGaugeValue()
    {
        // Arrange
        const string gaugeName = "test_gauge";
        const double gaugeValue = 42.5;

        // Act
        _metricsService.RecordGauge(gaugeName, gaugeValue);

        // Assert
        var snapshot = _metricsService.GetSnapshot();
        snapshot.Gauges.Should().ContainKey(gaugeName);
        snapshot.Gauges[gaugeName].Should().Be(gaugeValue);
    }

    [Fact]
    public void RecordGauge_UpdateExistingGauge_ShouldUpdateValue()
    {
        // Arrange
        const string gaugeName = "test_gauge";

        // Act
        _metricsService.RecordGauge(gaugeName, 10.0);
        _metricsService.RecordGauge(gaugeName, 20.0);

        // Assert
        var snapshot = _metricsService.GetSnapshot();
        snapshot.Gauges[gaugeName].Should().Be(20.0);
    }

    [Fact]
    public void GetSnapshot_MultipleMetrics_ShouldCalculateAverages()
    {
        // Arrange
        _metricsService.RecordTranscriptionDuration(TimeSpan.FromSeconds(1));
        _metricsService.RecordTranscriptionDuration(TimeSpan.FromSeconds(3));
        _metricsService.RecordTranscriptionDuration(TimeSpan.FromSeconds(5));

        // Act
        var snapshot = _metricsService.GetSnapshot();

        // Assert
        snapshot.TranscriptionCount.Should().Be(3);
        snapshot.AverageTranscriptionDuration.Should().BeCloseTo(TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void GetSnapshot_EmptyMetrics_ShouldReturnZeroValues()
    {
        // Act
        var snapshot = _metricsService.GetSnapshot();

        // Assert
        snapshot.TranscriptionCount.Should().Be(0);
        snapshot.ErrorCount.Should().Be(0);
        snapshot.AverageTranscriptionDuration.Should().Be(TimeSpan.Zero);
        snapshot.AverageLatency.Should().Be(TimeSpan.Zero);
        snapshot.TotalAudioDuration.Should().Be(TimeSpan.Zero);
        snapshot.Counters.Should().BeEmpty();
        snapshot.Gauges.Should().BeEmpty();
    }

    [Fact]
    public void Reset_ShouldClearAllMetrics()
    {
        // Arrange
        _metricsService.RecordTranscriptionDuration(TimeSpan.FromSeconds(1));
        _metricsService.RecordError("test_error");
        _metricsService.IncrementCounter("test_counter");
        _metricsService.RecordGauge("test_gauge", 10.0);

        // Act
        _metricsService.Reset();

        // Assert
        var snapshot = _metricsService.GetSnapshot();
        snapshot.TranscriptionCount.Should().Be(0);
        snapshot.ErrorCount.Should().Be(0);
        snapshot.Counters.Should().BeEmpty();
        snapshot.Gauges.Should().BeEmpty();
    }

    [Fact]
    public void GetSnapshot_MultipleErrors_ShouldCountAll()
    {
        // Arrange
        _metricsService.RecordError("error1");
        _metricsService.RecordError("error2");
        _metricsService.RecordError("error1");

        // Act
        var snapshot = _metricsService.GetSnapshot();

        // Assert
        snapshot.ErrorCount.Should().Be(3);
        snapshot.Counters["error.error1"].Should().Be(2);
        snapshot.Counters["error.error2"].Should().Be(1);
    }
}

