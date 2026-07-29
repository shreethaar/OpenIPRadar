namespace OpenIPRadar.Core.Models;

/// <summary>
/// A progress update emitted during a scan, suitable for driving a progress bar and status text.
/// </summary>
/// <param name="Completed">The number of IP addresses fully processed so far.</param>
/// <param name="Total">The total number of IP addresses in the scan.</param>
/// <param name="CurrentIp">The address most recently completed, if any.</param>
public sealed record ScanProgress(int Completed, int Total, string? CurrentIp)
{
    /// <summary>The completion fraction in the range 0.0–1.0.</summary>
    public double Fraction => Total <= 0 ? 0d : (double)Completed / Total;
}
