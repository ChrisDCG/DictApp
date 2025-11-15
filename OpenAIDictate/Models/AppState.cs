namespace OpenAIDictate.Models;

/// <summary>
/// Application state machine states
/// </summary>
public enum AppState
{
    /// <summary>Ready to start recording</summary>
    Idle,

    /// <summary>Currently recording audio</summary>
    Recording,

    /// <summary>Transcribing audio via OpenAI API</summary>
    Transcribing
}
