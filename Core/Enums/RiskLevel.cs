namespace OpenIPRadar.Core.Enums;

/// <summary>
/// A normalized risk classification derived from the aggregated provider results for an IP address.
/// </summary>
public enum RiskLevel
{
    /// <summary>No data was available to classify the address.</summary>
    Unknown,

    /// <summary>The address shows no signs of malicious activity.</summary>
    Clean,

    /// <summary>The address shows some indicators warranting caution.</summary>
    Suspicious,

    /// <summary>The address is strongly associated with malicious activity.</summary>
    Malicious
}
