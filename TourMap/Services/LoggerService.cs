using System.Diagnostics;
using System.Linq;

namespace TourMap.Services;

/// <summary>
/// Simple logging service implementation using System.Diagnostics
/// </summary>
public class LoggerService : ILoggerService
{
    private readonly string _category;

    public LoggerService(string category = "TourMap")
    {
        _category = category;
    }

    public void LogInformation(string message, params object[] args)
    {
        LogWithLevel(LogLevel.Information, message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        LogWithLevel(LogLevel.Warning, message, args);
    }

    public void LogError(string message, Exception? exception = null, params object[] args)
    {
        LogWithLevel(LogLevel.Error, message, args, exception);
    }

    public void LogDebug(string message, params object[] args)
    {
        LogWithLevel(LogLevel.Debug, message, args);
    }

    public void LogCritical(string message, Exception? exception = null, params object[] args)
    {
        LogWithLevel(LogLevel.Critical, message, args, exception);
    }

    private void LogWithLevel(LogLevel level, string message, object[] args, Exception? exception = null)
    {
        var formattedMessage = message;
        if (args.Length > 0)
        {
            try
            {
                formattedMessage = string.Format(message, args);
            }
            catch (FormatException)
            {
                // Support structured templates like "{DeviceId}" without throwing.
                var argDump = string.Join(", ", args.Select((a, i) => $"arg{i}={a}"));
                formattedMessage = $"{message} | {argDump}";
            }
        }

        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{_category}] [{level}] {formattedMessage}";
        
        if (exception != null)
        {
            logEntry += $"\nException: {exception}";
        }
        
        Debug.WriteLine(logEntry);
    }

    private enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error,
        Critical
    }
}
