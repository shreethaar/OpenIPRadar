using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Core.Enums;
using OpenIPRadar.Core.Models;

namespace OpenIPRadar.Services.Input;

/// <summary>
/// Extracts candidate IPv4/IPv6 tokens from arbitrary text using regular expressions, then
/// validates each with <see cref="IPAddress.TryParse(string, out IPAddress)"/> before accepting
/// it. Results are deduplicated by canonical form while preserving first-seen order.
/// </summary>
public sealed partial class IpExtractor : IIpExtractor
{
    /// <inheritdoc />
    public IReadOnlyList<IpAddressEntry> Extract(string rawText)
    {
        var results = new List<IpAddressEntry>();
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return results;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in CandidateRegex().Matches(rawText))
        {
            var token = match.Value;
            if (!IPAddress.TryParse(token, out var parsed))
            {
                continue;
            }

            // Canonical string form ensures equivalent representations dedupe correctly.
            var canonical = parsed.ToString();
            if (!seen.Add(canonical))
            {
                continue;
            }

            var version = parsed.AddressFamily == AddressFamily.InterNetworkV6
                ? IpVersion.IPv6
                : IpVersion.IPv4;

            results.Add(new IpAddressEntry(canonical, version));
        }

        return results;
    }

    /// <summary>
    /// Matches IPv4 dotted-quad and IPv6 (including compressed) candidate tokens. Precise
    /// validation is delegated to <see cref="IPAddress.TryParse(string, out IPAddress)"/>.
    /// </summary>
    [GeneratedRegex(
        @"(?<![\w.:])(?:\d{1,3}(?:\.\d{1,3}){3}|(?:[A-Fa-f0-9]{0,4}:){2,7}[A-Fa-f0-9]{0,4})(?![\w.:])")]
    private static partial Regex CandidateRegex();
}
