namespace OpenIPRadar.Core.Models;

/// <summary>
/// Describes a threat-intelligence provider for display and rate-limiting purposes.
/// </summary>
/// <param name="Name">The stable internal name (e.g. "AbuseIPDB").</param>
/// <param name="DisplayName">The human-friendly name shown in the UI.</param>
/// <param name="RequestsPerMinute">The provider's configured request budget per minute.</param>
/// <param name="IsEnabled">Whether the provider is enabled in configuration.</param>
public sealed record ProviderMetadata(
    string Name,
    string DisplayName,
    int RequestsPerMinute,
    bool IsEnabled);
