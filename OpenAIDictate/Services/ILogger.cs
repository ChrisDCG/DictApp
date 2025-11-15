namespace OpenAIDictate.Services;

/// <summary>
/// Structured logging interface for dependency injection
/// </summary>
public interface IAppLogger
{
    void LogInfo(string message, params object[] args);
    void LogDebug(string message, params object[] args);

    void LogWarning(string message, params object[] args);
    void LogWarning(Exception exception, string message, params object[] args);

    void LogError(string message, params object[] args);
    void LogError(Exception exception, string message, params object[] args);
}

