using System.Collections.ObjectModel;
using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Presentation.Mvvm;

namespace OpenIPRadar.Presentation.ViewModels;

/// <summary>
/// View model for the settings view: exposes one <see cref="ProviderKeyViewModel"/> per provider
/// so users can manage API keys and test connectivity.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    /// <summary>Initializes the settings view model.</summary>
    /// <param name="providers">All available providers.</param>
    /// <param name="keyStore">Secure key store.</param>
    /// <param name="logger">Application logger.</param>
    public SettingsViewModel(IEnumerable<IThreatProvider> providers, ISecureKeyStore keyStore, IAppLogger logger)
    {
        Providers = new ObservableCollection<ProviderKeyViewModel>(
            providers.Select(p => new ProviderKeyViewModel(p, keyStore, logger)));
    }

    /// <summary>The per-provider key configuration view models.</summary>
    public ObservableCollection<ProviderKeyViewModel> Providers { get; }
}
