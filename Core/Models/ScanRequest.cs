namespace OpenIPRadar.Core.Models;

/// <summary>
/// A request to scan a batch of IP addresses against a set of enabled providers.
/// </summary>
/// <param name="Addresses">The validated, deduplicated addresses to scan.</param>
/// <param name="EnabledProviderNames">The names of the providers to query for this scan.</param>
public sealed record ScanRequest(
    IReadOnlyList<IpAddressEntry> Addresses,
    IReadOnlyCollection<string> EnabledProviderNames);
