using OpenIPRadar.Core.Configuration;

namespace OpenIPRadar.Core.Abstractions;

/// <summary>
/// Provides access to the application's non-secret configuration, loaded from
/// <c>appsettings.json</c> and optional per-user overrides.
/// </summary>
public interface IConfigurationService
{
    /// <summary>The current, immutable application settings.</summary>
    AppSettings Settings { get; }

    /// <summary>Returns the settings for a named provider, or <c>null</c> if not configured.</summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>The provider settings, or <c>null</c>.</returns>
    ProviderSettings? GetProviderSettings(string providerName);
}
