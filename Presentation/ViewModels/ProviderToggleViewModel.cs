using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Presentation.Mvvm;

namespace OpenIPRadar.Presentation.ViewModels;

/// <summary>
/// Represents a provider's participation in the next scan. <see cref="IsConfigured"/> is live —
/// it re-queries the key store each time so that adding a key in the Settings tab immediately
/// enables the checkbox without requiring an app restart.
/// </summary>
public sealed class ProviderToggleViewModel : ObservableObject
{
    private readonly IThreatProvider _provider;
    private bool _isSelected;

    /// <summary>Initializes the toggle from a provider.</summary>
    /// <param name="provider">The provider represented by this toggle.</param>
    public ProviderToggleViewModel(IThreatProvider provider)
    {
        _provider = provider;
        Name = provider.Name;
        DisplayName = provider.Metadata.DisplayName;
        _isSelected = provider.Metadata.IsEnabled && provider.IsConfigured;
    }

    /// <summary>The provider's internal name.</summary>
    public string Name { get; }

    /// <summary>The provider's display name.</summary>
    public string DisplayName { get; }

    /// <summary>
    /// Whether the provider currently has a stored API key. Re-queried live so the checkbox
    /// enables immediately after a key is saved in the Settings tab.
    /// </summary>
    public bool IsConfigured => _provider.IsConfigured;

    /// <summary>Whether the provider is selected for the next scan.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// Refreshes <see cref="IsConfigured"/> and auto-selects the provider if a key was just
    /// added. Called by <see cref="SettingsViewModel"/> after a key is saved or cleared.
    /// </summary>
    public void RefreshConfigured()
    {
        OnPropertyChanged(nameof(IsConfigured));

        // Auto-enable selection when a key is first stored; leave it alone if the user
        // has deliberately unchecked it while a key was already present.
        if (IsConfigured && !_isSelected)
        {
            IsSelected = true;
        }
        else if (!IsConfigured)
        {
            IsSelected = false;
        }
    }
}
