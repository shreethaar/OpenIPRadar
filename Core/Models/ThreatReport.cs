using OpenIPRadar.Core.Enums;

namespace OpenIPRadar.Core.Models;

/// <summary>
/// The flattened, merged result for a single IP address across all providers.
/// This is the object bound to the results grid and rendered into HTML/PDF reports.
/// Each property corresponds to one display column; unavailable values are <c>null</c>
/// and rendered as "N/A" in the UI.
/// </summary>
public sealed record ThreatReport
{
    /// <summary>The IP address this report describes.</summary>
    public required string IpAddress { get; init; }

    /// <summary>Whether the address is IPv4 or IPv6.</summary>
    public required IpVersion Version { get; init; }

    /// <summary>Country (precedence: AbuseIPDB, then Shodan, then VirusTotal).</summary>
    public string? Country { get; init; }

    /// <summary>Autonomous System Number (precedence: Shodan, then AbuseIPDB, then VirusTotal).</summary>
    public string? Asn { get; init; }

    /// <summary>ISP or organization (precedence: AbuseIPDB, then Shodan).</summary>
    public string? Isp { get; init; }

    /// <summary>Hostname(s) (precedence: Shodan, then AbuseIPDB).</summary>
    public string? Hostname { get; init; }

    /// <summary>AbuseIPDB abuse-confidence score (0–100).</summary>
    public int? AbuseConfidence { get; init; }

    /// <summary>AbuseIPDB total report count.</summary>
    public int? Reports { get; init; }

    /// <summary>AbuseIPDB last-reported timestamp.</summary>
    public DateTimeOffset? LastReported { get; init; }

    /// <summary>VirusTotal reputation score.</summary>
    public int? VtReputation { get; init; }

    /// <summary>VirusTotal malicious engine count.</summary>
    public int? VtMalicious { get; init; }

    /// <summary>VirusTotal suspicious engine count.</summary>
    public int? VtSuspicious { get; init; }

    /// <summary>Shodan open ports.</summary>
    public IReadOnlyList<int>? OpenPorts { get; init; }

    /// <summary>Shodan-reported operating system.</summary>
    public string? OperatingSystem { get; init; }

    /// <summary>Aggregated tags across providers.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>The computed, normalized threat score (0–100).</summary>
    public int ThreatScore { get; init; }

    /// <summary>The risk classification derived from <see cref="ThreatScore"/>.</summary>
    public RiskLevel RiskLevel { get; init; }

    /// <summary>The individual provider results that were merged to build this report.</summary>
    public required IReadOnlyList<ProviderResult> ProviderResults { get; init; }
}
