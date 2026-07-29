using System.Windows.Input;
using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Presentation.Mvvm;

namespace OpenIPRadar.Presentation.ViewModels;

/// <summary>
/// View model for managing a single provider's API key: saving it (DPAPI-encrypted), clearing
/// it, and running a live connectivity test. Contains no persistence or HTTP logic itself — it
/// delegates to the injected key store and provider.
/// </summary>
public sealed class ProviderKeyViewModel : ObservableObject
{
    private readonly IThreatProvider _provider;
    private readonly ISecureKeyStore _keyStore;
    private readonly IAppLogger _logger;

    private string _apiKey = string.Empty;
    private string _status = string.Empty;
    private bool _isBusy;

    /// <summary>Initializes the view model for one provider.</summary>
    /// <param name="provider">The provider being configured.</param>
    /// <param name="keyStore">Secure key store.</param>
    /// <param name="logger">Application logger.</param>
    public ProviderKeyViewModel(IThreatProvider provider, ISecureKeyStore keyStore, IAppLogger logger)
    {
        _provider = provider;
        _keyStore = keyStore;
        _logger = logger;

        HasStoredKey = keyStore.HasKey(provider.Name);
        _status = HasStoredKey ? "Key stored." : "No key stored.";

        SaveCommand = new RelayCommand(Save, () => !string.IsNullOrWhiteSpace(ApiKey) && !IsBusy);
        ClearCommand = new RelayCommand(Clear, () => HasStoredKey && !IsBusy);
        TestCommand = new AsyncRelayCommand(TestAsync, () => HasStoredKey && !IsBusy, OnError);
    }

    /// <summary>The provider's display name.</summary>
    public string DisplayName => _provider.Metadata.DisplayName;

    /// <summary>Whether a key is currently stored for this provider.</summary>
    public bool HasStoredKey { get; private set; }

    /// <summary>The API key entered by the user (write-only in practice; never pre-filled).</summary>
    public string ApiKey
    {
        get => _apiKey;
        set => SetProperty(ref _apiKey, value);
    }

    /// <summary>A short status/result message shown next to the provider.</summary>
    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    /// <summary>Whether a connectivity test is in progress.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>Encrypts and stores the entered key.</summary>
    public ICommand SaveCommand { get; }

    /// <summary>Removes the stored key.</summary>
    public ICommand ClearCommand { get; }

    /// <summary>Runs a connectivity/authentication test.</summary>
    public ICommand TestCommand { get; }

    /// <summary>
    /// Invoked after a key is saved or cleared so the Scan tab's provider toggles can refresh
    /// their <c>IsConfigured</c> state without requiring an app restart.
    /// </summary>
    public Action? OnKeyChanged { get; set; }

    private void Save()
    {
        _keyStore.SetKey(_provider.Name, ApiKey.Trim());
        HasStoredKey = true;
        OnPropertyChanged(nameof(HasStoredKey));
        ApiKey = string.Empty;
        Status = "Key saved.";
        _logger.Information($"API key updated for {_provider.Name}.");
        OnKeyChanged?.Invoke();
    }

    private void Clear()
    {
        _keyStore.RemoveKey(_provider.Name);
        HasStoredKey = false;
        OnPropertyChanged(nameof(HasStoredKey));
        Status = "Key cleared.";
        _logger.Information($"API key removed for {_provider.Name}.");
        OnKeyChanged?.Invoke();
    }

    private async Task TestAsync()
    {
        IsBusy = true;
        Status = "Testing…";
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var ok = await _provider.TestConnectivityAsync(cts.Token).ConfigureAwait(true);
            Status = ok ? "Connection OK." : "Connection failed — check the key.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnError(Exception ex)
    {
        Status = "Test error.";
        _logger.Error($"Connectivity test failed for {_provider.Name}.", ex);
    }
}
