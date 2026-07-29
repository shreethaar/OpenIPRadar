using OpenIPRadar.Core.Models;

namespace OpenIPRadar.Core.Abstractions;

/// <summary>
/// The central contract implemented by every threat-intelligence provider (AbuseIPDB,
/// VirusTotal, Shodan, …). New providers are added by implementing this interface and
/// registering the implementation in the composition root.
/// </summary>
public interface IThreatProvider
{
    /// <summary>The stable internal name of the provider (e.g. "AbuseIPDB").</summary>
    string Name { get; }

    /// <summary>Descriptive metadata used for display and rate limiting.</summary>
    ProviderMetadata Metadata { get; }

    /// <summary>Whether a usable API key is currently configured for this provider.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Queries the provider for reputation data about a single IP address. Implementations
    /// must never throw for provider/network errors; they return a failed
    /// <see cref="ProviderResult"/> instead.
    /// </summary>
    /// <param name="ip">The address to check.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The provider's result for the address.</returns>
    Task<ProviderResult> CheckAsync(IpAddressEntry ip, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies that the configured API key and endpoint are reachable and valid.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns><c>true</c> if connectivity and authentication succeed; otherwise <c>false</c>.</returns>
    Task<bool> TestConnectivityAsync(CancellationToken cancellationToken);
}
