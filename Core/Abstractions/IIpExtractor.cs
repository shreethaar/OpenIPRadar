using OpenIPRadar.Core.Models;

namespace OpenIPRadar.Core.Abstractions;

/// <summary>
/// Extracts, validates, and deduplicates IPv4 and IPv6 addresses from arbitrary raw text.
/// </summary>
public interface IIpExtractor
{
    /// <summary>
    /// Parses the supplied text, returning distinct, valid IP addresses in first-seen order.
    /// </summary>
    /// <param name="rawText">The text to scan (pasted content, or the contents of a TXT/CSV file).</param>
    /// <returns>The deduplicated, validated addresses.</returns>
    IReadOnlyList<IpAddressEntry> Extract(string rawText);
}
