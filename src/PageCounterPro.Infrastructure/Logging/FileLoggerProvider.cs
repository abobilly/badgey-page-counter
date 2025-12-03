namespace PageCounterPro.Infrastructure.Logging;

using Microsoft.Extensions.Logging;

/// <summary>
/// Custom file logger provider for application logging.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private const string AppName = "PageCounterPro";
    private readonly string _logDirectory;
    private readonly StreamWriter _writer;
    private readonly object _lock = new();

    public FileLoggerProvider()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _logDirectory = Path.Combine(localAppData, AppName, "Logs");

        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }

        var logFileName = $"PageCounterPro_{DateTime.Now:yyyyMMdd}.log";
        var logFilePath = Path.Combine(_logDirectory, logFileName);

        _writer = new StreamWriter(logFilePath, append: true)
        {
            AutoFlush = true
        };

        // Cleanup old log files (keep last 7 days)
        CleanupOldLogs();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _writer, _lock);
    }

    public void Dispose()
    {
        _writer.Dispose();
    }

    private void CleanupOldLogs()
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-7);
            var logFiles = Directory.GetFiles(_logDirectory, "PageCounterPro_*.log");

            foreach (var file in logFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    fileInfo.Delete();
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

/// <summary>
/// File-based logger implementation.
/// </summary>
public sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly StreamWriter _writer;
    private readonly object _lock;

    public FileLogger(string categoryName, StreamWriter writer, object lockObject)
    {
        _categoryName = categoryName;
        _writer = writer;
        _lock = lockObject;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = logLevel.ToString().ToUpper().PadRight(11);
            var message = formatter(state, exception);

            _writer.WriteLine($"[{timestamp}] [{level}] [{_categoryName}] {message}");

            if (exception != null)
            {
                _writer.WriteLine($"    Exception: {exception}");
            }
        }
    }
}
