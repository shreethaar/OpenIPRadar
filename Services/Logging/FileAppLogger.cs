using System.IO;
using System.Threading.Channels;
using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Core.Configuration;

namespace OpenIPRadar.Services.Logging;

/// <summary>
/// A thread-safe file logger that writes to a daily rolling file under the configured
/// directory. Log entries are queued on an unbounded channel and drained by a single
/// background writer, so logging never blocks the UI or scan threads. API keys must never
/// be passed to this logger.
/// </summary>
public sealed class FileAppLogger : IAppLogger, IAsyncDisposable
{
    private readonly Channel<string> _channel;
    private readonly Task _writerTask;
    private readonly string _directory;
    private readonly int _minLevel;

    /// <summary>Initializes the logger and starts the background writer.</summary>
    /// <param name="settings">Logging settings (directory and minimum level).</param>
    public FileAppLogger(LoggingSettings settings)
    {
        _directory = Environment.ExpandEnvironmentVariables(
            settings.Directory.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(_directory);
        _minLevel = ParseLevel(settings.MinimumLevel);

        _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _writerTask = Task.Run(DrainAsync);
    }

    /// <inheritdoc />
    public void Debug(string message) => Write(0, "DEBUG", message);

    /// <inheritdoc />
    public void Information(string message) => Write(1, "INFO", message);

    /// <inheritdoc />
    public void Warning(string message) => Write(2, "WARN", message);

    /// <inheritdoc />
    public void Error(string message, Exception? exception = null)
    {
        var full = exception is null ? message : $"{message}{Environment.NewLine}{exception}";
        Write(3, "ERROR", full);
    }

    private void Write(int level, string label, string message)
    {
        if (level < _minLevel)
        {
            return;
        }

        var line = $"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff 'UTC'}] [{label}] {message}";
        // TryWrite always succeeds on an unbounded channel that is still open.
        _channel.Writer.TryWrite(line);
    }

    private async Task DrainAsync()
    {
        await foreach (var line in _channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                var path = Path.Combine(_directory, $"log-{DateTime.UtcNow:yyyyMMdd}.txt");
                await File.AppendAllTextAsync(path, line + Environment.NewLine).ConfigureAwait(false);
            }
            catch
            {
                // Logging must never throw or crash the application; drop the line on failure.
            }
        }
    }

    private static int ParseLevel(string level) => level.Trim().ToLowerInvariant() switch
    {
        "debug" => 0,
        "information" or "info" => 1,
        "warning" or "warn" => 2,
        "error" => 3,
        _ => 1
    };

    /// <summary>Flushes and stops the background writer.</summary>
    /// <returns>A task that completes once all queued entries are written.</returns>
    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        try
        {
            await _writerTask.ConfigureAwait(false);
        }
        catch
        {
            // Ignore shutdown errors.
        }
    }
}
