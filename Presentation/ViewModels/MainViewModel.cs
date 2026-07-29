using System.Collections.ObjectModel;
using System.Windows.Input;
using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Core.Enums;
using OpenIPRadar.Core.Models;
using OpenIPRadar.Presentation.Mvvm;

namespace OpenIPRadar.Presentation.ViewModels;

/// <summary>
/// The primary view model driving the scan workflow: gather input (paste or file), extract and
/// deduplicate IPs, select providers, run the scan with live progress and cancellation, display
/// the merged results, and export them to HTML or PDF. All long-running work is asynchronous so
/// the UI thread is never blocked, and all business logic is delegated to injected services.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly IIpExtractor _ipExtractor;
    private readonly IInputFileReader _fileReader;
    private readonly IScanOrchestrator _orchestrator;
    private readonly IReportModelBuilder _reportModelBuilder;
    private readonly IReadOnlyList<IReportExporter> _exporters;
    private readonly IDialogService _dialogService;
    private readonly IAppLogger _logger;

    private CancellationTokenSource? _scanCts;
    private string _inputText = string.Empty;
    private string _statusMessage = "Ready.";
    private double _progressValue;
    private bool _isScanning;

    /// <summary>Initializes the main view model.</summary>
    /// <param name="ipExtractor">IP extraction service.</param>
    /// <param name="fileReader">Input file reader.</param>
    /// <param name="orchestrator">Scan orchestrator.</param>
    /// <param name="reportModelBuilder">Report model builder.</param>
    /// <param name="exporters">Available report exporters.</param>
    /// <param name="dialogService">Dialog service for file pickers and messages.</param>
    /// <param name="settings">The settings view model.</param>
    /// <param name="providers">All available providers.</param>
    /// <param name="logger">Application logger.</param>
    public MainViewModel(
        IIpExtractor ipExtractor,
        IInputFileReader fileReader,
        IScanOrchestrator orchestrator,
        IReportModelBuilder reportModelBuilder,
        IEnumerable<IReportExporter> exporters,
        IDialogService dialogService,
        SettingsViewModel settings,
        IEnumerable<IThreatProvider> providers,
        IAppLogger logger)
    {
        _ipExtractor = ipExtractor;
        _fileReader = fileReader;
        _orchestrator = orchestrator;
        _reportModelBuilder = reportModelBuilder;
        _exporters = exporters.ToList();
        _dialogService = dialogService;
        _logger = logger;
        Settings = settings;

        Providers = new ObservableCollection<ProviderToggleViewModel>(
            providers.Select(p => new ProviderToggleViewModel(p)));

        // When a key is saved or cleared in the Settings tab, refresh the matching scan-tab toggle
        // so IsConfigured and IsSelected update immediately without an app restart.
        // Both collections are built from the same ordered provider list, so index == index.
        for (var i = 0; i < Settings.Providers.Count && i < Providers.Count; i++)
        {
            var captured = Providers[i];
            Settings.Providers[i].OnKeyChanged = captured.RefreshConfigured;
        }

        LoadFileCommand = new AsyncRelayCommand(LoadFileAsync, () => !IsScanning, OnError);
        ExtractCommand = new RelayCommand(Extract, () => !IsScanning);
        ClearCommand = new RelayCommand(Clear, () => !IsScanning);
        ScanCommand = new AsyncRelayCommand(ScanAsync, CanScan, OnError);
        CancelCommand = new RelayCommand(Cancel, () => IsScanning);
        ExportHtmlCommand = new AsyncRelayCommand(() => ExportAsync(ReportFormat.Html), CanExport, OnError);
        ExportPdfCommand = new AsyncRelayCommand(() => ExportAsync(ReportFormat.Pdf), CanExport, OnError);
    }

    /// <summary>The settings view model (API keys).</summary>
    public SettingsViewModel Settings { get; }

    /// <summary>The provider selection toggles for the next scan.</summary>
    public ObservableCollection<ProviderToggleViewModel> Providers { get; }

    /// <summary>The IP addresses extracted from the current input.</summary>
    public ObservableCollection<IpAddressEntry> ExtractedIps { get; } = new();

    /// <summary>The results of the most recent scan.</summary>
    public ObservableCollection<ThreatReport> Reports { get; } = new();

    /// <summary>Raw input text (pasted content or loaded file text).</summary>
    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    /// <summary>A short status message shown in the UI.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Scan progress as a percentage (0–100).</summary>
    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    /// <summary>Whether a scan is currently running.</summary>
    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (SetProperty(ref _isScanning, value))
            {
                OnPropertyChanged(nameof(IsNotScanning));
            }
        }
    }

    /// <summary>Convenience inverse of <see cref="IsScanning"/> for binding.</summary>
    public bool IsNotScanning => !IsScanning;

    /// <summary>Loads IPs from a TXT/CSV file.</summary>
    public ICommand LoadFileCommand { get; }

    /// <summary>Extracts IPs from the current input text.</summary>
    public ICommand ExtractCommand { get; }

    /// <summary>Clears input and results.</summary>
    public ICommand ClearCommand { get; }

    /// <summary>Runs the scan.</summary>
    public ICommand ScanCommand { get; }

    /// <summary>Cancels a running scan.</summary>
    public ICommand CancelCommand { get; }

    /// <summary>Exports the results as HTML.</summary>
    public ICommand ExportHtmlCommand { get; }

    /// <summary>Exports the results as PDF.</summary>
    public ICommand ExportPdfCommand { get; }

    private async Task LoadFileAsync()
    {
        var path = _dialogService.OpenInputFile();
        if (path is null)
        {
            return;
        }

        StatusMessage = "Reading file…";
        InputText = await _fileReader.ReadAsync(path, CancellationToken.None).ConfigureAwait(true);
        Extract();
    }

    private void Extract()
    {
        var ips = _ipExtractor.Extract(InputText);
        ExtractedIps.Clear();
        foreach (var ip in ips)
        {
            ExtractedIps.Add(ip);
        }

        StatusMessage = $"{ExtractedIps.Count} unique IP address(es) ready.";
    }

    private void Clear()
    {
        InputText = string.Empty;
        ExtractedIps.Clear();
        Reports.Clear();
        ProgressValue = 0;
        StatusMessage = "Ready.";
    }

    private bool CanScan() => !IsScanning && ExtractedIps.Count > 0 && Providers.Any(p => p.IsSelected);

    private bool CanExport() => !IsScanning && Reports.Count > 0;

    private async Task ScanAsync()
    {
        var enabled = Providers.Where(p => p.IsSelected).Select(p => p.Name).ToList();
        if (enabled.Count == 0)
        {
            _dialogService.ShowMessage("No providers", "Select at least one provider to scan.", isError: true);
            return;
        }

        IsScanning = true;
        Reports.Clear();
        ProgressValue = 0;
        _scanCts = new CancellationTokenSource();

        // Created on the UI thread, so Report callbacks marshal back to the UI thread.
        var progress = new Progress<ScanProgress>(p =>
        {
            ProgressValue = p.Fraction * 100d;
            StatusMessage = $"Scanning {p.Completed}/{p.Total}… ({p.CurrentIp})";
        });

        try
        {
            var request = new ScanRequest(ExtractedIps.ToList(), enabled);
            var results = await _orchestrator.ScanAsync(request, progress, _scanCts.Token).ConfigureAwait(true);

            foreach (var report in results)
            {
                Reports.Add(report);
            }

            StatusMessage = $"Scan complete: {Reports.Count} result(s).";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled.";
        }
        finally
        {
            _scanCts.Dispose();
            _scanCts = null;
            IsScanning = false;
        }
    }

    private void Cancel()
    {
        _scanCts?.Cancel();
        StatusMessage = "Cancelling…";
    }

    private async Task ExportAsync(ReportFormat format)
    {
        var exporter = _exporters.FirstOrDefault(e => e.Format == format);
        if (exporter is null)
        {
            return;
        }

        var path = _dialogService.SaveReportFile(format);
        if (path is null)
        {
            return;
        }

        StatusMessage = $"Exporting {format}…";
        var model = _reportModelBuilder.Build(Reports.ToList());
        await exporter.ExportAsync(model, path, CancellationToken.None).ConfigureAwait(true);
        StatusMessage = $"Report exported: {path}";
        _dialogService.ShowMessage("Export complete", $"Report saved to:\n{path}");
    }

    private void OnError(Exception ex)
    {
        IsScanning = false;
        StatusMessage = "An error occurred.";
        _logger.Error("Unhandled error in main view model.", ex);
        _dialogService.ShowMessage("Error", ex.Message, isError: true);
    }
}
