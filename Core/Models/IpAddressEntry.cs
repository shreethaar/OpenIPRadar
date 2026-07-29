using OpenIPRadar.Core.Enums;

namespace OpenIPRadar.Core.Models;

/// <summary>
/// A validated, deduplicated IP address extracted from user input, ready to be scanned.
/// </summary>
/// <param name="Address">The canonical string form of the IP address.</param>
/// <param name="Version">Whether the address is IPv4 or IPv6.</param>
public sealed record IpAddressEntry(string Address, IpVersion Version);
