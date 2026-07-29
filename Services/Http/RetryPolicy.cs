using OpenIPRadar.Core.Abstractions;

namespace OpenIPRadar.Services.Http;

/// <summary>
/// A minimal exponential-backoff retry helper for transient failures, replacing an external
/// resilience library. The caller decides which exceptions are transient.
/// </summary>
public sealed class RetryPolicy
{
    private readonly int _maxAttempts;
    private readonly TimeSpan _baseDelay;
    private readonly IAppLogger _logger;

    /// <summary>Initializes the policy.</summary>
    /// <param name="maxRetries">Maximum number of retries after the initial attempt.</param>
    /// <param name="baseDelay">Base delay used for exponential backoff.</param>
    /// <param name="logger">Logger for retry diagnostics.</param>
    public RetryPolicy(int maxRetries, TimeSpan baseDelay, IAppLogger logger)
    {
        _maxAttempts = Math.Max(1, maxRetries + 1);
        _baseDelay = baseDelay;
        _logger = logger;
    }

    /// <summary>
    /// Executes <paramref name="action"/>, retrying transient failures with exponential backoff.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="action">The operation to execute.</param>
    /// <param name="isTransient">Predicate identifying retryable exceptions.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The operation result.</returns>
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        Func<Exception, bool> isTransient,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < _maxAttempts && isTransient(ex))
            {
                var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                _logger.Warning($"Transient failure (attempt {attempt}/{_maxAttempts}): {ex.Message}. Retrying in {delay.TotalMilliseconds:F0} ms.");
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
