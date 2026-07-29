using System.IO;
using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Presentation.Services;
using OpenIPRadar.Presentation.ViewModels;
using OpenIPRadar.Providers.AbuseIpDb;
using OpenIPRadar.Providers.Shodan;
using OpenIPRadar.Providers.VirusTotal;
using OpenIPRadar.Services.Configuration;
using OpenIPRadar.Services.Http;
using OpenIPRadar.Services.Input;
using OpenIPRadar.Services.Logging;
using OpenIPRadar.Services.RateLimiting;
using OpenIPRadar.Services.Reporting;
using OpenIPRadar.Services.Scanning;
using OpenIPRadar.Services.Security;

namespace OpenIPRadar;

/// <summary>
/// The application's manual composition root. It constructs the entire object graph once at
/// startup — replacing an IoC container with explicit, transparent wiring — and owns the
/// disposable infrastructure (logger, HTTP client) for orderly shutdown. Adding a new provider
/// requires only one additional line in the <c>providers</c> list.
/// </summary>
public sealed class CompositionRoot : IAsyncDisposable
{
    private readonly FileAppLogger _logger;
    private readonly SharedHttpClient _httpClient;

    /// <summary>The fully-wired main view model, ready to be bound to the main window.</summary>
    public MainViewModel MainViewModel { get; }

    /// <summary>Builds the object graph.</summary>
    public CompositionRoot()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var userDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenIPRadar");
        Directory.CreateDirectory(userDirectory);

        var configuration = new ConfigurationService(baseDirectory, userDirectory);
        _logger = new FileAppLogger(configuration.Settings.Logging);
        var keyStore = new DpapiKeyStore(userDirectory);
        var rateLimiter = new TokenBucketRateLimiter(configuration);
        var retryPolicy = new RetryPolicy(
            configuration.Settings.Scanning.RetryCount,
            TimeSpan.FromMilliseconds(configuration.Settings.Scanning.RetryBaseDelayMs),
            _logger);
        _httpClient = new SharedHttpClient(configuration.Settings.Scanning);

        // Providers are registered here as a simple list; the orchestrator receives them all.
        var providers = new List<IThreatProvider>
        {
            new AbuseIpDbProvider(configuration, keyStore, rateLimiter, retryPolicy, _httpClient, _logger),
            new VirusTotalProvider(configuration, keyStore, rateLimiter, retryPolicy, _httpClient, _logger),
            new ShodanProvider(configuration, keyStore, rateLimiter, retryPolicy, _httpClient, _logger)
        };

        var riskAggregator = new RiskAggregator();
        var orchestrator = new ScanOrchestrator(providers, riskAggregator, configuration, _logger);
        var exporters = new List<IReportExporter>
        {
            new HtmlReportExporter(),
            new PdfReportExporter()
        };

        var settingsViewModel = new SettingsViewModel(providers, keyStore, _logger);

        MainViewModel = new MainViewModel(
            new IpExtractor(),
            new InputFileReader(),
            orchestrator,
            new ReportModelBuilder(),
            exporters,
            new DialogService(),
            settingsViewModel,
            providers,
            _logger);

        _logger.Information("Application composition complete.");
    }

    /// <summary>Disposes owned infrastructure, flushing pending log entries.</summary>
    /// <returns>A task that completes once shutdown finishes.</returns>
    public async ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        await _logger.DisposeAsync().ConfigureAwait(false);
    }
}
