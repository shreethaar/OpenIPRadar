using OpenIPRadar.Core.Models;

namespace OpenIPRadar.Core.Abstractions;

/// <summary>
/// Coordinates a batch scan: fans each address out across the enabled providers with
/// bounded concurrency, merges the per-provider results into a single report per address,
/// reports progress, and honors cancellation.
/// </summary>
public interface IScanOrchestrator
{
    /// <summary>
    /// Executes the scan described by <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The addresses and enabled providers to scan.</param>
    /// <param name="progress">An optional sink that receives incremental progress updates.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>One merged <see cref="ThreatReport"/> per scanned address.</returns>
    Task<IReadOnlyList<ThreatReport>> ScanAsync(
        ScanRequest request,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken);
}
