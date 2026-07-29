namespace OpenIPRadar.Core.Configuration;

/// <summary>
/// The strongly-typed root of <c>appsettings.json</c>, bound at startup.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Scan-wide execution settings.</summary>
    public ScanningSettings Scanning { get; set; } = new();

    /// <summary>Per-provider settings keyed by provider name (e.g. "AbuseIPDB").</summary>
    public Dictionary<string, ProviderSettings> Providers { get; set; } = new();

    /// <summary>Logging settings.</summary>
    public LoggingSettings Logging { get; set; } = new();
}

/// <summary>
/// Settings that govern how a batch scan is executed.
/// </summary>
public sealed class ScanningSettings
{
    /// <summary>Maximum number of IP addresses processed concurrently.</summary>
    public int MaxConcurrency { get; set; } = 4;

    /// <summary>Per-request HTTP timeout, in seconds.</summary>
    public int HttpTimeoutSeconds { get; set; } = 30;

    /// <summary>Maximum retry attempts for transient failures.</summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>Base delay for exponential backoff between retries, in milliseconds.</summary>
    public int RetryBaseDelayMs { get; set; } = 500;
}

/// <summary>
/// Settings for a single threat-intelligence provider. Endpoint URLs and paths live here
/// (never hardcoded) so they can be changed without recompiling.
/// </summary>
public sealed class ProviderSettings
{
    /// <summary>Whether the provider participates in scans.</summary>
    public bool Enabled { get; set; }

    /// <summary>The provider's API base URL (must end with a trailing slash).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>AbuseIPDB check endpoint path, relative to <see cref="BaseUrl"/>.</summary>
    public string? CheckPath { get; set; }

    /// <summary>AbuseIPDB maximum report age, in days.</summary>
    public int MaxAgeInDays { get; set; } = 90;

    /// <summary>VirusTotal IP-lookup endpoint path, relative to <see cref="BaseUrl"/>.</summary>
    public string? IpLookupPath { get; set; }

    /// <summary>Shodan host-lookup endpoint path, relative to <see cref="BaseUrl"/>.</summary>
    public string? HostPath { get; set; }

    /// <summary>Shodan api-info endpoint path (used for the free connectivity/credit test).</summary>
    public string? ApiInfoPath { get; set; }

    /// <summary>The provider's request budget per minute, enforced by the rate limiter.</summary>
    public int RequestsPerMinute { get; set; } = 60;
}

/// <summary>
/// Settings that govern application logging.
/// </summary>
public sealed class LoggingSettings
{
    /// <summary>The minimum level to log ("Debug", "Information", "Warning", "Error").</summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>The directory (supporting environment-variable expansion) where log files are written.</summary>
    public string Directory { get; set; } = "%AppData%/OpenIPRadar/logs";
}
