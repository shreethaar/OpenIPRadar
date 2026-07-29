using System.Collections.Concurrent;
using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Core.Enums;
using OpenIPRadar.Core.Models;
using OpenIPRadar.Providers.AbuseIpDb;
using OpenIPRadar.Providers.Shodan;
using OpenIPRadar.Providers.VirusTotal;

namespace OpenIPRadar.Services.Scanning;

/// <summary>
/// Coordinates a batch scan. Each address is processed with bounded concurrency; within an
/// address the enabled providers are queried in parallel (each provider throttles itself via the
/// rate limiter). The per-provider results are then merged into a single flat
/// <see cref="ThreatReport"/> using the agreed column precedence, and the threat score is
/// computed. Progress is reported after each completed address and cancellation is honored.
/// </summary>
public sealed class ScanOrchestrator : IScanOrchestrator
{
    private readonly IReadOnlyList<IThreatProvider> _providers;
    private readonly IRiskAggregator _riskAggregator;
    private readonly IConfigurationService _configuration;
    private readonly IAppLogger _logger;

    /// <summary>Initializes the orchestrator.</summary>
    /// <param name="providers">All available providers.</param>
    /// <param name="riskAggregator">Threat-score calculator.</param>
    /// <param name="configuration">Configuration (concurrency settings).</param>
    /// <param name="logger">Application logger.</param>
    public ScanOrchestrator(
        IEnumerable<IThreatProvider> providers,
        IRiskAggregator riskAggregator,
        IConfigurationService configuration,
        IAppLogger logger)
    {
        _providers = providers.ToList();
        _riskAggregator = riskAggregator;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ThreatReport>> ScanAsync(
        ScanRequest request,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        // Participation is governed by the user's per-scan selection; the configuration
        // "Enabled" flag only seeds the default checkbox state in the UI.
        var activeProviders = _providers
            .Where(p => request.EnabledProviderNames.Contains(p.Name))
            .ToList();

        _logger.Information(
            $"Starting scan: {request.Addresses.Count} address(es), providers: " +
            $"{string.Join(", ", activeProviders.Select(p => p.Name))}.");

        var reports = new ConcurrentDictionary<string, ThreatReport>();
        var completed = 0;
        var total = request.Addresses.Count;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, _configuration.Settings.Scanning.MaxConcurrency),
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(request.Addresses, options, async (ip, ct) =>
        {
            var results = await Task.WhenAll(
                activeProviders.Select(p => p.CheckAsync(ip, ct))).ConfigureAwait(false);

            var report = Merge(ip, results);
            reports[ip.Address] = report;

            var done = Interlocked.Increment(ref completed);
            progress?.Report(new ScanProgress(done, total, ip.Address));
        }).ConfigureAwait(false);

        _logger.Information($"Scan complete: {reports.Count} report(s) produced.");

        // Preserve the original input order in the returned list.
        return request.Addresses
            .Select(ip => reports[ip.Address])
            .ToList();
    }

    /// <summary>
    /// Merges the per-provider results for one address into the flat report, applying the
    /// column-source precedence and computing the aggregate threat score.
    /// </summary>
    private ThreatReport Merge(IpAddressEntry ip, IReadOnlyList<ProviderResult> results)
    {
        var byProvider = results.ToDictionary(r => r.ProviderName, StringComparer.OrdinalIgnoreCase);

        ProviderResult? abuse = Get(byProvider, AbuseIpDbProvider.ProviderName);
        ProviderResult? vt = Get(byProvider, VirusTotalProvider.ProviderName);
        ProviderResult? shodan = Get(byProvider, ShodanProvider.ProviderName);

        var (score, level) = _riskAggregator.Evaluate(results);

        var tags = results
            .SelectMany(r => r.Tags ?? Enumerable.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ThreatReport
        {
            IpAddress = ip.Address,
            Version = ip.Version,
            // Precedence per column, as agreed in the design.
            Country = FirstNonEmpty(abuse?.Country, shodan?.Country, vt?.Country),
            Asn = FirstNonEmpty(shodan?.Asn, abuse?.Asn, vt?.Asn),
            Isp = FirstNonEmpty(abuse?.Isp, shodan?.Isp, vt?.Isp),
            Hostname = JoinHostnames(shodan?.Hostnames ?? abuse?.Hostnames),
            AbuseConfidence = abuse?.AbuseConfidenceScore,
            Reports = abuse?.TotalReports,
            LastReported = abuse?.LastReportedAt,
            VtReputation = vt?.VtReputation,
            VtMalicious = vt?.VtMalicious,
            VtSuspicious = vt?.VtSuspicious,
            OpenPorts = shodan?.OpenPorts,
            OperatingSystem = shodan?.OperatingSystem,
            Tags = tags.Count == 0 ? null : tags,
            ThreatScore = score,
            RiskLevel = level,
            ProviderResults = results
        };
    }

    private static ProviderResult? Get(IReadOnlyDictionary<string, ProviderResult> map, string name) =>
        map.TryGetValue(name, out var result) && result.Status == ProviderResultStatus.Success ? result : null;

    private static string? FirstNonEmpty(params string?[] candidates) =>
        candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));

    private static string? JoinHostnames(IReadOnlyList<string>? hostnames) =>
        hostnames is { Count: > 0 } ? string.Join(", ", hostnames) : null;
}
