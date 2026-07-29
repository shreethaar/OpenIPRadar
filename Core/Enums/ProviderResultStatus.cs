namespace OpenIPRadar.Core.Enums;

/// <summary>
/// Represents the outcome of querying a single threat-intelligence provider for one IP address.
/// </summary>
public enum ProviderResultStatus
{
    /// <summary>The provider returned data successfully.</summary>
    Success,

    /// <summary>The provider returned an error (network failure, malformed response, unexpected status).</summary>
    Error,

    /// <summary>The provider rejected the request because a rate limit or quota was exceeded.</summary>
    RateLimited,

    /// <summary>The provider was not queried because no valid API key is configured.</summary>
    NotConfigured,

    /// <summary>The provider was intentionally skipped (disabled by the user or in configuration).</summary>
    Skipped
}
