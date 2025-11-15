using System.Globalization;

namespace OpenAIDictate.Models;

/// <summary>
/// Application configuration stored in %APPDATA%\OpenAIDictate\config.json
/// </summary>
public class AppConfig
{
    /// <summary>
    /// DPAPI-encrypted OpenAI API key (not used if OPENAI_API_KEY env var is set)
    /// </summary>
    public string? ApiKeyEncrypted { get; set; }

    /// <summary>
    /// OpenAI transcription model (default: gpt-4o-transcribe, the latest SOTA model from March 2025)
    /// Can be overridden by OPENAI_TRANSCRIBE_MODEL environment variable
    /// </summary>
    public string Model { get; set; } = "gpt-4o-transcribe";

    /// <summary>
    /// Hotkey gesture for start/stop recording (default: F5)
    /// Supported keys: F1-F12, or combinations like "Ctrl+F5", "Alt+F5", "Shift+F5"
    /// </summary>
    public string HotkeyGesture { get; set; } = "F5";

    /// <summary>
    /// Parsed hotkey modifiers (cached for performance)
    /// </summary>
    public int HotkeyModifiers { get; set; } = 0;

    /// <summary>
    /// Parsed virtual key code (cached for performance)
    /// </summary>
    public int HotkeyVirtualKey { get; set; } = 0x74; // F5 default

    /// <summary>
    /// Maximum recording duration in minutes (default: 10)
    /// </summary>
    public int MaxRecordingMinutes { get; set; } = 10;

    /// <summary>
    /// Language code for transcription (e.g., "de" for German, "en" for English)
    /// Improves accuracy when specified
    /// </summary>
    public string? Language { get; set; } = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

    /// <summary>
    /// Custom glossary for proper nouns, legal terms, product names, etc.
    /// Used to improve transcription accuracy for domain-specific terminology
    /// </summary>
    public string? Glossary { get; set; }

    /// <summary>
    /// Enable LLM post-processing for punctuation and formatting (default: true)
    /// Uses GPT-4o-mini to add punctuation, capitalization, and symbols
    /// </summary>
    public bool EnablePostProcessing { get; set; } = true;

    /// <summary>
    /// Enable server-side auto chunking (OpenAI server VAD) for long-form audio.
    /// Required for diarization requests exceeding 30 seconds and recommended for optimal accuracy.
    /// </summary>
    public bool EnableServerAutoChunking { get; set; } = true;

    /// <summary>
    /// Request token-level log probabilities from the transcription API (gpt-4o models only).
    /// Useful for monitoring confidence and regression detection.
    /// </summary>
    public bool IncludeLogProbabilities { get; set; } = false;

    /// <summary>
    /// When the diarization model is selected, request diarized JSON output that contains speaker segments.
    /// </summary>
    public bool RequestDiarizedOutput { get; set; } = true;

    /// <summary>
    /// Enable Voice Activity Detection (VAD) for better transcription quality (default: true)
    /// Removes silence and non-speech segments before transcription
    /// </summary>
    public bool EnableVAD { get; set; } = true;

    /// <summary>
    /// Silence threshold in dBFS for audio trimming (default: -20.0)
    /// Values closer to 0 are louder, more negative values detect quieter sounds
    /// </summary>
    public double SilenceThresholdDb { get; set; } = -20.0;

    /// <summary>
    /// Preferred UI culture (e.g., "en-US", "de-DE") for localized user interface strings.
    /// Defaults to the current UI culture of the operating system.
    /// </summary>
    public string UiCulture { get; set; } = CultureInfo.CurrentUICulture.Name;

    /// <summary>
    /// Speech probability threshold for Silero VAD (default: 0.5 as recommended by Silero documentation).
    /// Higher values make VAD stricter, lower values allow quieter speech to pass through.
    /// </summary>
    public double VadSpeechThreshold { get; set; } = 0.5;

    /// <summary>
    /// Minimum silence duration (in milliseconds) before closing a speech segment.
    /// Default aligns with Silero guidance (100 ms).
    /// </summary>
    public int VadMinSilenceDurationMs { get; set; } = 120;

    /// <summary>
    /// Minimum speech duration (in milliseconds) required to keep a segment (default: 250 ms).
    /// Prevents spurious spikes from being treated as speech.
    /// </summary>
    public int VadMinSpeechDurationMs { get; set; } = 250;

    /// <summary>
    /// Padding (in milliseconds) added to both sides of each detected speech segment (default: 60 ms).
    /// This mimics the official Silero padding recommendations.
    /// </summary>
    public int VadSpeechPaddingMs { get; set; } = 60;
}
