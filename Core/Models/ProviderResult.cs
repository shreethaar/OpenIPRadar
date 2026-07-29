using OpenIPRadar.Core.Enums;

namespace OpenIPRadar.Core.Models;

/// <summary>
/// The raw, per-provider outcome of checking a single IP address. Several of these
/// (one per provider) are merged into a single <see cref="ThreatReport"/> for display.
/// </summary>
public sealed record ProviderResult
{
    /// <summary>The name of the provider that produced this result (e.g. "AbuseIPDB").</summary>
    public required string ProviderName { get; init; }

    /// <summary>The outcome status of the query.</summary>
    public required ProviderResultStatus Status { get; init; }

    /// <summary>An error message when <see cref="Status"/> is not <see cref="ProviderResultStatus.Success"/>; otherwise <c>null</c>.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>The UTC timestamp at which the provider was queried.</summary>
    public DateTimeOffset QueriedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>The country name or code reported by the provider, if any.</summary>
    public string? Country { get; init; }

    /// <summary>The Autonomous System Number reported by the provider, if any.</summary>
    public string? Asn { get; init; }

    /// <summary>The ISP or organization reported by the provider, if any.</summary>
    public string? Isp { get; init; }

    /// <summary>Hostnames associated with the address, if any.</summary>
    public IReadOnlyList<string>? Hostnames { get; init; }

    /// <summary>Tags associated with the address, if any.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>AbuseIPDB abuse-confidence score (0–100), if applicable.</summary>
    public int? AbuseConfidenceScore { get; init; }

    /// <summary>AbuseIPDB total report count, if applicable.</summary>
    public int? TotalReports { get; init; }

    /// <summary>AbuseIPDB last-reported timestamp, if applicable.</summary>
    public DateTimeOffset? LastReportedAt { get; init; }

    /// <summary>VirusTotal reputation score, if applicable.</summary>
    public int? VtReputation { get; init; }

    /// <summary>VirusTotal count of engines flagging the address as malicious, if applicable.</summary>
    public int? VtMalicious { get; init; }

    /// <summary>VirusTotal count of engines flagging the address as suspicious, if applicable.</summary>
    public int? VtSuspicious { get; init; }

    /// <summary>Shodan open ports, if applicable.</summary>
    public IReadOnlyList<int>? OpenPorts { get; init; }

    /// <summary>Shodan-reported operating system, if applicable.</summary>
    public string? OperatingSystem { get; init; }

    /// <summary>Creates a non-success result for the given provider and status.</summary>
    /// <param name="providerName">The provider name.</param>
    /// <param name="status">The failure or skip status.</param>
    /// <param name="errorMessage">An optional human-readable explanation.</param>
    /// <returns>A <see cref="ProviderResult"/> carrying only the failure metadata.</returns>
    public static ProviderResult Failure(string providerName, ProviderResultStatus status, string? errorMessage = null) =>
        new() { ProviderName = providerName, Status = status, ErrorMessage = errorMessage };
}
