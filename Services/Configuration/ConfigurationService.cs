using System.IO;
using System.Text.Json;
using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Core.Configuration;

namespace OpenIPRadar.Services.Configuration;

/// <summary>
/// Loads application configuration from the shipped <c>appsettings.json</c> and, when present,
/// overlays a per-user <c>settings.json</c> from <c>%AppData%\OpenIPRadar</c>. Parsing uses
/// <see cref="System.Text.Json"/> only (no external configuration libraries).
/// </summary>
public sealed class ConfigurationService : IConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public AppSettings Settings { get; }

    /// <summary>Loads configuration from disk.</summary>
    /// <param name="baseDirectory">The directory containing the shipped <c>appsettings.json</c>.</param>
    /// <param name="userDirectory">The per-user override directory (e.g. <c>%AppData%\OpenIPRadar</c>).</param>
    public ConfigurationService(string baseDirectory, string userDirectory)
    {
        var settings = Load(Path.Combine(baseDirectory, "appsettings.json")) ?? new AppSettings();

        var userPath = Path.Combine(userDirectory, "settings.json");
        var userSettings = Load(userPath);
        if (userSettings is not null)
        {
            Merge(settings, userSettings);
        }

        Settings = settings;
    }

    /// <inheritdoc />
    public ProviderSettings? GetProviderSettings(string providerName) =>
        Settings.Providers.TryGetValue(providerName, out var provider) ? provider : null;

    private static AppSettings? Load(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null;
        }
    }

    /// <summary>Overlays user-provided values on top of the shipped defaults.</summary>
    private static void Merge(AppSettings baseSettings, AppSettings overrides)
    {
        baseSettings.Scanning = overrides.Scanning;
        baseSettings.Logging = overrides.Logging;
        foreach (var (name, provider) in overrides.Providers)
        {
            baseSettings.Providers[name] = provider;
        }
    }
}
