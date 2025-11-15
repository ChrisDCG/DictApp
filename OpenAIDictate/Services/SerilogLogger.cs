using Serilog;
using Serilog.Events;

namespace OpenAIDictate.Services;

/// <summary>
/// Enterprise-grade structured logging using Serilog
/// Logs to %APPDATA%\OpenAIDictate\logs\app_YYYY-MM-DD.log
/// </summary>
public sealed class SerilogLogger : IAppLogger, IDisposable
{
    private readonly ILogger _logger;
    private static bool _isInitialized;

    public SerilogLogger()
    {
        if (!_isInitialized)
        {
            InitializeSerilog();
            _isInitialized = true;
        }

        _logger = Log.Logger;
    }

    private static void InitializeSerilog()
    {
        string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenAIDictate",
            "logs"
        );

        Directory.CreateDirectory(logDirectory);

        string logFilePath = Path.Combine(logDirectory, "app_.log");
        string logFilePattern = Path.Combine(logDirectory, "app_{Date}.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "OpenAIDictate")
            .Enrich.WithProperty("Version", "1.1.0")
            .WriteTo.File(
                path: logFilePattern,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: 10_000_000, // 10 MB
                rollOnFileSizeLimit: true
            )
            .WriteTo.File(
                path: Path.Combine(logDirectory, "app_compact.jsonl"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                formatter: new Serilog.Formatting.Compact.CompactJsonFormatter(),
                fileSizeLimitBytes: 10_000_000
            )
            .CreateLogger();

        Log.Information("Serilog initialized. Logging to: {LogDirectory}", logDirectory);
    }

    public void LogInfo(string message, params object[] args) =>
        LogInternal(
            _logger.Information,
            (msg, parameters) => _logger.Information(messageTemplate: msg, propertyValues: parameters),
            message,
            args);

    public void LogDebug(string message, params object[] args) =>
        LogInternal(
            _logger.Debug,
            (msg, parameters) => _logger.Debug(messageTemplate: msg, propertyValues: parameters),
            message,
            args);

    public void LogWarning(string message, params object[] args) =>
        LogInternal(
            _logger.Warning,
            (msg, parameters) => _logger.Warning(messageTemplate: msg, propertyValues: parameters),
            message,
            args);

    public void LogWarning(Exception exception, string message, params object[] args)
    {
        if (args.Length > 0)
        {
            _logger.Warning(exception, message, args);
        }
        else
        {
            _logger.Warning(exception, message);
        }
    }

    public void LogError(string message, params object[] args) =>
        LogInternal(
            _logger.Error,
            (msg, parameters) => _logger.Error(messageTemplate: msg, propertyValues: parameters),
            message,
            args);

    public void LogError(Exception exception, string message, params object[] args)
    {
        if (args.Length > 0)
        {
            _logger.Error(exception, message, args);
        }
        else
        {
            _logger.Error(exception, message);
        }
    }

    private static void LogInternal(
        Action<string> logWithoutArgs,
        Action<string, object[]> logWithArgs,
        string message,
        object[] args)
    {
        if (args.Length > 0)
        {
            logWithArgs(message, args);
        }
        else
        {
            logWithoutArgs(message);
        }
    }

    public void Dispose()
    {
        Log.CloseAndFlush();
    }
}

