using Serilog;

namespace OpenAIDictate.Services;

/// <summary>
/// Static logger wrapper for convenience in utility classes
/// Uses Serilog internally for unified logging
/// Note: Ensure Serilog is initialized before using this logger
/// </summary>
public static class Logger
{
    /// <summary>
    /// Log an informational message
    /// </summary>
    public static void LogInfo(string message)
    {
        try
        {
            Log.Information(message);
        }
        catch
        {
            // Silent fail - logging should never crash the application
        }
    }

    /// <summary>
    /// Log an error message
    /// </summary>
    public static void LogError(string message)
    {
        try
        {
            Log.Error(message);
        }
        catch
        {
            // Silent fail - logging should never crash the application
        }
    }

    /// <summary>
    /// Log a warning message
    /// </summary>
    public static void LogWarning(string message)
    {
        try
        {
            Log.Warning(message);
        }
        catch
        {
            // Silent fail - logging should never crash the application
        }
    }
}
