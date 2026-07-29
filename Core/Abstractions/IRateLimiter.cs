namespace OpenIPRadar.Core.Abstractions;

/// <summary>
/// Throttles outbound requests on a per-provider basis so configured rate limits are respected.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Asynchronously waits until the named provider is permitted to issue another request.
    /// </summary>
    /// <param name="providerName">The provider requesting a slot.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task that completes when a request slot is available.</returns>
    Task WaitAsync(string providerName, CancellationToken cancellationToken);
}
