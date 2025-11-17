using System.Buffers;
using NAudio.Wave;
using OpenAIDictate.Models;

namespace OpenAIDictate.Services;

/// <summary>
/// Records audio from the default microphone using NAudio
/// Optimized for OpenAI Whisper API: 16kHz, 16-bit PCM, Mono, WAV format
/// Audio is kept in RAM only and never written to disk
/// </summary>
public class AudioRecorder : IDisposable
{
    // Optimal settings for Whisper API (based on OpenAI best practices)
    private const int TargetSampleRate = 16000;  // 16kHz optimal for speech recognition
    private const int BitsPerSample = 16;         // 16-bit PCM
    private const int Channels = 1;               // Mono (reduces file size, sufficient for speech)
    private const int BufferMilliseconds = 20;    // Low latency buffer

    private WaveInEvent? _waveIn;
    private MemoryStream? _recordingStream;
    private WaveFileWriter? _waveWriter;
    private WaveStream? _resamplerStream;
    private readonly AppConfig _config;
    private DateTime _recordingStartTime;

    public AudioRecorder(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Starts recording audio from the default microphone
    /// </summary>
    public void StartRecording()
    {
        if (_waveIn != null)
        {
            throw new InvalidOperationException("Recording is already in progress");
        }

        try
        {
            // Create WaveIn device for capturing audio
            _waveIn = new WaveInEvent
            {
                BufferMilliseconds = BufferMilliseconds,
                NumberOfBuffers = 2
            };

            // Try to set optimal format (16kHz, 16-bit, mono)
            WaveFormat targetFormat = new WaveFormat(TargetSampleRate, BitsPerSample, Channels);
            
            try
            {
                _waveIn.WaveFormat = targetFormat;
            }
            catch
            {
                // Device doesn't support 16kHz, will use device default and resample
                Logger.LogWarning($"Device doesn't support {TargetSampleRate}Hz format. Will use device default and resample.");
            }

            // Get actual device format (may differ from target)
            WaveFormat deviceFormat = _waveIn.WaveFormat;
            
            // Create in-memory stream to store audio (no disk I/O)
            _recordingStream = new MemoryStream();

            // Check if resampling is needed
            bool needsResampling = deviceFormat.SampleRate != TargetSampleRate || 
                                  deviceFormat.Channels != Channels || 
                                  deviceFormat.BitsPerSample != BitsPerSample;

            if (needsResampling)
            {
                Logger.LogInfo($"Device format: {deviceFormat.SampleRate}Hz/{deviceFormat.Channels}ch/{deviceFormat.BitsPerSample}bit. Will resample to {TargetSampleRate}Hz/{Channels}ch/{BitsPerSample}bit.");
            }

            // Create WAV writer with target format (resampling will happen in OnDataAvailable)
            _waveWriter = new WaveFileWriter(_recordingStream, targetFormat);

            // Handle data available events
            _waveIn.DataAvailable += OnDataAvailable;

            // Start recording
            _recordingStartTime = DateTime.Now;
            _waveIn.StartRecording();

            Logger.LogInfo($"Recording started: {targetFormat.SampleRate}Hz, {targetFormat.BitsPerSample}-bit, {targetFormat.Channels} channel(s)");
        }
        catch (Exception ex)
        {
            Cleanup();
            Logger.LogError($"Failed to start recording: {ex.Message}");
            throw new InvalidOperationException($"Failed to start recording. Please check microphone permissions and availability. Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Stops recording and returns the audio data as a WAV stream
    /// </summary>
    public async Task<MemoryStream> StopRecordingAsync()
    {
        if (_waveIn == null || _recordingStream == null || _waveWriter == null)
        {
            throw new InvalidOperationException("No recording in progress");
        }

        try
        {
            // Stop recording
            _waveIn.StopRecording();
            _waveIn.DataAvailable -= OnDataAvailable;

            // Calculate duration
            TimeSpan duration = DateTime.Now - _recordingStartTime;
            Logger.LogInfo($"Recording stopped. Duration: {duration.TotalSeconds:F2}s, Size: {_recordingStream.Length} bytes");

            // Finalize WAV file
            await _waveWriter.FlushAsync();
            _waveWriter.Dispose();
            _waveWriter = null;

            // Reset stream position for reading
            _recordingStream.Position = 0;

            // Return the stream (caller is responsible for disposing)
            var result = _recordingStream;
            _recordingStream = null; // Prevent disposal in Cleanup

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error stopping recording: {ex.Message}");
            throw;
        }
        finally
        {
            Cleanup();
        }
    }

    /// <summary>
    /// Checks if recording has exceeded maximum duration
    /// </summary>
    public bool HasExceededMaxDuration()
    {
        if (_waveIn == null || _waveIn.WaveFormat == null)
        {
            return false;
        }

        TimeSpan elapsed = DateTime.Now - _recordingStartTime;
        return elapsed.TotalMinutes >= _config.MaxRecordingMinutes;
    }

    /// <summary>
    /// Gets current recording duration
    /// </summary>
    public TimeSpan GetRecordingDuration()
    {
        if (_waveIn == null)
        {
            return TimeSpan.Zero;
        }

        return DateTime.Now - _recordingStartTime;
    }

    /// <summary>
    /// Checks if currently recording
    /// </summary>
    public bool IsRecording => _waveIn != null;

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_waveWriter == null || _waveIn == null || e.BytesRecorded == 0)
        {
            return;
        }

        try
        {
            WaveFormat deviceFormat = _waveIn.WaveFormat;
            WaveFormat targetFormat = new WaveFormat(TargetSampleRate, BitsPerSample, Channels);

            // Check if resampling is needed
            if (deviceFormat.SampleRate != TargetSampleRate || 
                deviceFormat.Channels != Channels || 
                deviceFormat.BitsPerSample != BitsPerSample)
            {
                // Resample using NAudio's WaveFormatConversionStream
                ResampleAndWrite(e.Buffer, e.BytesRecorded, deviceFormat, targetFormat);
            }
            else
            {
                // Direct write (no resampling needed)
                _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error writing audio data: {ex.Message}");
            // Don't throw - continue recording
        }
    }

    /// <summary>
    /// Resamples audio data from device format to target format (16kHz, 16-bit, mono)
    /// Uses ArrayPool for efficient buffer management
    /// </summary>
    private void ResampleAndWrite(byte[] buffer, int bytesRecorded, WaveFormat sourceFormat, WaveFormat targetFormat)
    {
        if (_waveWriter == null)
        {
            return;
        }

        // Use ArrayPool to reduce GC pressure
        int bufferSize = targetFormat.AverageBytesPerSecond / 10; // 100ms buffer
        byte[]? resampledBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        
        try
        {
            // Create a temporary stream with source format data
            using var sourceStream = new MemoryStream(buffer, 0, bytesRecorded, writable: false);
            using var rawSource = new RawSourceWaveStream(sourceStream, sourceFormat);
            using var resampler = new WaveFormatConversionStream(targetFormat, rawSource);
            
            // Read resampled data and write to output
            int bytesRead;
            while ((bytesRead = resampler.Read(resampledBuffer, 0, bufferSize)) > 0)
            {
                _waveWriter.Write(resampledBuffer, 0, bytesRead);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Resampling failed: {ex.Message}. Audio may have incorrect format.");
            // Fallback: try direct write (may cause issues, but better than losing data)
            try
            {
                _waveWriter.Write(buffer, 0, bytesRecorded);
            }
            catch
            {
                // Ignore - format mismatch expected
            }
        }
        finally
        {
            // Return buffer to pool
            if (resampledBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(resampledBuffer);
            }
        }
    }

    private void Cleanup()
    {
        try
        {
            _waveIn?.Dispose();
            _waveIn = null;

            _waveWriter?.Dispose();
            _waveWriter = null;

            _resamplerStream?.Dispose();
            _resamplerStream = null;

            _recordingStream?.Dispose();
            _recordingStream = null;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error during cleanup: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }
}
