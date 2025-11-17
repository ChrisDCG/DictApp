using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NAudio.Wave;
using OpenAIDictate.Models;

namespace OpenAIDictate.Services;

/// <summary>
/// Handles audio transcription using OpenAI API
/// Implements best practices from OpenAI Cookbook for maximum accuracy
/// </summary>
public class TranscriptionService
{
    private const string ApiEndpoint = "https://api.openai.com/v1/audio/transcriptions";
    private readonly HttpClient _httpClient;
    private readonly AppConfig _config;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly IAppLogger _logger;
    private readonly IMetricsService _metrics;
    private readonly Random _random = new(); // Reusable Random instance for retry jitter
    private readonly AudioPreprocessor _audioPreprocessor;
    private bool _logProbabilitiesUnsupportedLogged;
    private bool _diarizedOutputUnsupportedLogged;
    private bool _serverChunkingUnsupportedLogged;

    public TranscriptionService(AppConfig config, string apiKey, IAppLogger? logger = null, IMetricsService? metrics = null, AudioPreprocessor? audioPreprocessor = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _logger = logger ?? new SerilogLogger();
        _metrics = metrics ?? new MetricsService(_logger);
        _model = ConfigService.GetModel(config);
        _audioPreprocessor = audioPreprocessor ?? new AudioPreprocessor(_config);

        _httpClient = OpenAIHttpClientFactory.Create(TimeSpan.FromMinutes(5));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        _logger.LogInfo("TranscriptionService initialized with model: {Model}", _model);
    }

    /// <summary>
    /// Transcribes audio using OpenAI API with best practices for accuracy
    /// </summary>
    /// <param name="audioStream">WAV audio stream (16kHz, 16-bit, mono)</param>
    /// <returns>Transcribed text</returns>
    public async Task<string> TranscribeAsync(Stream audioStream)
    {
        if (audioStream == null || audioStream.Length == 0)
        {
            throw new ArgumentException("Audio stream is empty", nameof(audioStream));
        }

        _logger.LogInfo("Starting transcription. Audio size: {Size} bytes", audioStream.Length);
        var transcriptionStartTime = DateTime.UtcNow;

        Stream? preprocessedStream = null;
        Stream streamToUse = audioStream; // Track which stream to actually use
        try
        {
            // Preprocess audio (silence trimming, VAD) if enabled
            if (_config.EnableVAD)
            {
                preprocessedStream = await _audioPreprocessor.PreprocessAsync(audioStream);
                _logger.LogInfo("Audio preprocessed. New size: {Size} bytes", preprocessedStream.Length);

                // Use preprocessed stream for validation and upload
                streamToUse = preprocessedStream;
            }
            else
            {
                streamToUse = audioStream;
            }

            streamToUse.Position = 0;

            // Check file size (OpenAI limit: 25MB) before attempting any decoding
            const long maxSizeBytes = 25 * 1024 * 1024; // 25 MB
            if (streamToUse.Length > maxSizeBytes)
            {
                throw new InvalidOperationException(
                    $"Audio file too large ({streamToUse.Length / 1024 / 1024}MB). OpenAI API limit is 25MB. " +
                    "Please reduce recording duration."
                );
            }

            streamToUse.Position = 0;

            // Validate audio format before uploading
            AudioFormatValidator.Validate(streamToUse);
            streamToUse.Position = 0;
            _logger.LogInfo("Audio format validated (16kHz/16-bit PCM mono)");

            // Build transcription prompt using advanced strategies (GPT-generated or basic)
            string prompt = await BuildTranscriptionPromptAsync();

            byte[] audioBytes = await ReadStreamToByteArrayAsync(streamToUse);
            RecordAudioDuration(audioBytes);

            string responseFormat = DetermineResponseFormat();
            bool includeLogProbs = ShouldIncludeLogProbabilities();
            bool useServerChunking = ShouldUseServerAutoChunking();

            var startTime = DateTime.UtcNow;
            using HttpResponseMessage response = await SendWithRetryAsync(
                () => CreateMultipartContent(audioBytes, prompt, responseFormat, includeLogProbs, useServerChunking)
            );
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordTranscriptionLatency(duration);

            // Handle response
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _metrics.RecordError($"http_{response.StatusCode}");
                _logger.LogError("OpenAI API error: HTTP {StatusCode} - {ResponseBody}", (int)response.StatusCode, responseBody);
                throw new InvalidOperationException(
                    $"OpenAI API request failed (HTTP {(int)response.StatusCode}): {GetErrorMessage(responseBody)}"
                );
            }

            // Parse JSON response
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            string transcription = ExtractTranscript(doc.RootElement);

            if (includeLogProbs)
            {
                CaptureLogProbabilities(doc.RootElement);
            }
            else
            {
                _metrics.RecordGauge("transcription.logprob.mean", double.NaN);
                _metrics.RecordGauge("transcription.logprob.min", double.NaN);
            }

            if (string.Equals(responseFormat, "diarized_json", StringComparison.OrdinalIgnoreCase))
            {
                RecordDiarizationMetrics(doc.RootElement);
            }
            else
            {
                _metrics.RecordGauge("transcription.diarized_segments", 0);
            }

            if (string.IsNullOrWhiteSpace(transcription))
            {
                throw new InvalidOperationException("OpenAI API returned empty transcription");
            }

            var totalDuration = DateTime.UtcNow - transcriptionStartTime;
            _metrics.RecordTranscriptionDuration(totalDuration);
            _logger.LogInfo("Transcription completed in {Duration:F2}s. Length: {Length} chars", totalDuration.TotalSeconds, transcription.Length);

            // Apply post-processing if enabled
            if (_config.EnablePostProcessing && !string.Equals(responseFormat, "diarized_json", StringComparison.OrdinalIgnoreCase))
            {
                transcription = await ApplyPostProcessingAsync(transcription);
            }
            else if (_config.EnablePostProcessing)
            {
                _logger.LogInfo("Skipping post-processing for diarized output to preserve speaker labels");
            }

            return transcription;
        }
        catch (HttpRequestException ex)
        {
            _metrics.RecordError("network_error");
            _logger.LogError(ex, "Network error during transcription: {Message}", ex.Message);
            throw new InvalidOperationException(
                "Network error while connecting to OpenAI API. Please check your internet connection.", ex
            );
        }
        catch (TaskCanceledException ex)
        {
            _metrics.RecordError("timeout");
            _logger.LogError(ex, "Transcription timeout: {Message}", ex.Message);
            throw new InvalidOperationException("Transcription request timed out. Please try again with a shorter recording.", ex);
        }
        finally
        {
            // Dispose preprocessed stream if we created it
            // Note: We always dispose it if it exists, since it's a separate stream from the original audioStream
            if (preprocessedStream != null)
            {
                try
                {
                    preprocessedStream.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to dispose preprocessed stream: {Message}", ex.Message);
                }
            }
        }
    }

    /// <summary>
    /// Builds transcription prompt using basic strategy
    /// </summary>
    private Task<string> BuildTranscriptionPromptAsync()
    {
        // Use simple prompt - GPT-4o-mini-transcribe works great without complex prompts
        return Task.FromResult(BuildBasicPrompt());
    }

    /// <summary>
    /// Builds basic transcription prompt (fallback method)
    /// </summary>
    private string BuildBasicPrompt()
    {
        var promptParts = new List<string>();

        // Add custom glossary if specified (for proper nouns, legal terms, etc.)
        if (!string.IsNullOrWhiteSpace(_config.Glossary))
        {
            // OpenAI best practice: Use natural sentences instead of just lists
            promptParts.Add($"Fachbegriffe: {_config.Glossary}.");
        }

        // Add example sentence in desired style
        // OpenAI Cookbook: "Long prompts may be more reliable at steering Whisper"
        if (_config.Language == "de")
        {
            promptParts.Add(
                "Der Bundesgerichtshof entschied über Schadensersatz gemäß §§ 280, 241 Abs. 2 BGB bezüglich der Willenserklärung im Bürgschaftsvertrag."
            );
        }
        else
        {
            promptParts.Add(
                "The court decided on damages according to relevant statutory provisions regarding contractual obligations."
            );
        }

        string prompt = string.Join(" ", promptParts);

        // OpenAI limits prompt to last 224 tokens
        if (prompt.Length > 1000)
        {
            prompt = prompt.Substring(prompt.Length - 1000);
        }

        return prompt;
    }

    /// <summary>
    /// Applies LLM-based post-processing for punctuation and formatting
    /// Uses GPT-4o-mini as recommended by OpenAI Cookbook
    /// </summary>
    private async Task<string> ApplyPostProcessingAsync(string rawTranscription)
    {
        try
        {
            _logger.LogInfo("Applying post-processing (punctuation, formatting)");

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "Add punctuation to text. Preserve original words exactly. " +
                                  "Insert periods, commas, capitalization, symbols (€, %, §, etc.). " +
                                  "Do not change, add, or remove any words."
                    },
                    new
                    {
                        role = "user",
                        content = rawTranscription
                    }
                },
                temperature = 0, // Deterministic
                max_tokens = 4000
            };

            string json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Post-processing failed: HTTP {StatusCode}. Returning raw transcription.", (int)response.StatusCode);
                return rawTranscription;
            }

            using JsonDocument doc = JsonDocument.Parse(responseBody);
            string? processed = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return processed ?? rawTranscription;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Post-processing error: {Message}. Returning raw transcription.", ex.Message);
            return rawTranscription; // Fallback to raw transcription
        }
    }

    private async Task<byte[]> ReadStreamToByteArrayAsync(Stream stream)
    {
        stream.Position = 0;
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private MultipartFormDataContent CreateMultipartContent(
        byte[] audioBytes,
        string prompt,
        string responseFormat,
        bool includeLogProbabilities,
        bool useServerChunking)
    {
        var content = new MultipartFormDataContent();

        var audioContent = new ByteArrayContent(audioBytes);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(audioContent, "file", "audio.wav");

        content.Add(new StringContent(_model), "model");

        if (useServerChunking)
        {
            content.Add(new StringContent("auto"), "chunking_strategy");
        }

        if (!string.IsNullOrWhiteSpace(_config.Language))
        {
            content.Add(new StringContent(_config.Language), "language");
        }

        content.Add(new StringContent("0"), "temperature");

        if (!string.IsNullOrWhiteSpace(prompt))
        {
            content.Add(new StringContent(prompt), "prompt");
        }

        content.Add(new StringContent(responseFormat), "response_format");

        if (includeLogProbabilities)
        {
            content.Add(new StringContent("logprobs"), "include[]");
        }

        return content;
    }

    private string DetermineResponseFormat()
    {
        if (_config.RequestDiarizedOutput)
        {
            if (ModelSupportsDiarizedOutput())
            {
                return "diarized_json";
            }

            if (!_diarizedOutputUnsupportedLogged)
            {
                _logger.LogWarning(
                    "Diarized output requested but model {Model} does not support diarization. Falling back to JSON.",
                    _model
                );
                _diarizedOutputUnsupportedLogged = true;
            }
        }

        return "json";
    }

    private bool ShouldIncludeLogProbabilities()
    {
        if (!_config.IncludeLogProbabilities)
        {
            return false;
        }

        if (!ModelSupportsLogProbabilities())
        {
            if (!_logProbabilitiesUnsupportedLogged)
            {
                _logger.LogWarning(
                    "Log probabilities requested but model {Model} does not support them. Setting will be ignored.",
                    _model
                );
                _logProbabilitiesUnsupportedLogged = true;
            }

            return false;
        }

        return true;
    }

    private bool ShouldUseServerAutoChunking()
    {
        if (!_config.EnableServerAutoChunking)
        {
            return false;
        }

        if (!ModelSupportsServerChunking())
        {
            if (!_serverChunkingUnsupportedLogged)
            {
                _logger.LogWarning(
                    "Server auto chunking requested but model {Model} does not support chunking_strategy. Skipping.",
                    _model
                );
                _serverChunkingUnsupportedLogged = true;
            }

            return false;
        }

        return true;
    }

    private bool ModelSupportsLogProbabilities()
    {
        if (_model.Contains("diarize", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return _model.StartsWith("gpt-4o-transcribe", StringComparison.OrdinalIgnoreCase) ||
               _model.StartsWith("gpt-4o-mini-transcribe", StringComparison.OrdinalIgnoreCase);
    }

    private bool ModelSupportsDiarizedOutput()
    {
        return _model.Contains("diarize", StringComparison.OrdinalIgnoreCase);
    }

    private bool ModelSupportsServerChunking()
    {
        return _model.StartsWith("gpt-4o", StringComparison.OrdinalIgnoreCase);
    }

    private void RecordAudioDuration(byte[] audioBytes)
    {
        try
        {
            using var memoryStream = new MemoryStream(audioBytes);
            using var reader = new WaveFileReader(memoryStream);
            _metrics.RecordAudioDuration(reader.TotalTime);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Unable to determine audio duration: {Message}", ex.Message);
        }
    }

    private static string ExtractTranscript(JsonElement root)
    {
        if (root.TryGetProperty("text", out JsonElement textElement))
        {
            return textElement.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private void CaptureLogProbabilities(JsonElement root)
    {
        if (!root.TryGetProperty("logprobs", out JsonElement logprobs) || logprobs.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        double sum = 0;
        double min = double.MaxValue;
        int count = 0;

        foreach (JsonElement element in logprobs.EnumerateArray())
        {
            double? value = TryGetLogProbability(element);
            if (value.HasValue)
            {
                double logProb = value.Value;
                sum += logProb;
                min = Math.Min(min, logProb);
                count++;
            }
        }

        if (count > 0)
        {
            _metrics.RecordGauge("transcription.logprob.mean", sum / count);
            _metrics.RecordGauge("transcription.logprob.min", min);
        }
    }

    private static double? TryGetLogProbability(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out double numberValue))
        {
            return numberValue;
        }

        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("logprob", out JsonElement logprobElement) &&
            logprobElement.TryGetDouble(out double logprob))
        {
            return logprob;
        }

        return null;
    }

    private void RecordDiarizationMetrics(JsonElement root)
    {
        if (root.TryGetProperty("segments", out JsonElement segments) && segments.ValueKind == JsonValueKind.Array)
        {
            _metrics.RecordGauge("transcription.diarized_segments", segments.GetArrayLength());
        }
    }

    /// <summary>
    /// Sends HTTP request with retry logic (3 attempts, exponential backoff with jitter)
    /// Implements best practices: exponential backoff with jitter to prevent thundering herd
    /// </summary>
    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpContent> contentFactory, int maxRetries = 3)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            using HttpContent content = contentFactory();
            try
            {
                var response = await _httpClient.PostAsync(ApiEndpoint, content);

                // Retry on 429 (Too Many Requests) or 5xx errors
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                    ((int)response.StatusCode >= 500 && (int)response.StatusCode < 600))
                {
                    if (attempt < maxRetries)
                    {
                        int baseDelayMs = (int)Math.Pow(2, attempt) * 1000; // Exponential: 2s, 4s, 8s
                        // Add jitter (±25%) to prevent synchronized retries
                        int jitterMs = _random.Next(-baseDelayMs / 4, baseDelayMs / 4);
                        int delayMs = baseDelayMs + jitterMs;

                        _logger.LogWarning("Request failed with HTTP {StatusCode} (attempt {Attempt}/{MaxRetries}). Retrying in {DelayMs}ms...", (int)response.StatusCode, attempt, maxRetries, delayMs);
                        response.Dispose();
                        await Task.Delay(delayMs);
                        continue;
                    }
                }

                return response;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                lastException = ex;
                int baseDelayMs = (int)Math.Pow(2, attempt) * 1000; // Exponential: 2s, 4s, 8s
                // Add jitter (±25%) to prevent synchronized retries
                int jitterMs = _random.Next(-baseDelayMs / 4, baseDelayMs / 4);
                int delayMs = baseDelayMs + jitterMs;

                _logger.LogWarning("Request failed (attempt {Attempt}/{MaxRetries}): {Message}. Retrying in {DelayMs}ms...", attempt, maxRetries, ex.Message, delayMs);
                await Task.Delay(delayMs);
            }
            catch (TaskCanceledException ex) when (attempt < maxRetries && ex.InnerException is TimeoutException)
            {
                lastException = ex;
                int delayMs = (int)Math.Pow(2, attempt) * 1000;
                _logger.LogWarning("Request timeout (attempt {Attempt}/{MaxRetries}). Retrying in {DelayMs}ms...", attempt, maxRetries, delayMs);
                await Task.Delay(delayMs);
            }
        }

        throw new InvalidOperationException($"Request failed after {maxRetries} attempts", lastException);
    }

    /// <summary>
    /// Extracts error message from OpenAI API error response
    /// </summary>
    private static string GetErrorMessage(string responseBody)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("error").GetProperty("message").GetString() ?? responseBody;
        }
        catch
        {
            // Truncate long error messages
            return responseBody.Length > 200 ? responseBody.Substring(0, 200) + "..." : responseBody;
        }
    }
}
