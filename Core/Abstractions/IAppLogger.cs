namespace OpenIPRadar.Core.Abstractions;

/// <summary>
/// A minimal logging abstraction so the rest of the application never depends on a
/// concrete logging implementation. Implementations must never log secrets (API keys).
/// </summary>
public interface IAppLogger
{
    /// <summary>Logs a debug-level message.</summary>
    /// <param name="message">The message to log.</param>
    void Debug(string message);

    /// <summary>Logs an informational message.</summary>
    /// <param name="message">The message to log.</param>
    void Information(string message);

    /// <summary>Logs a warning.</summary>
    /// <param name="message">The message to log.</param>
    void Warning(string message);

    /// <summary>Logs an error, optionally with an associated exception.</summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The associated exception, if any.</param>
    void Error(string message, Exception? exception = null);
}
