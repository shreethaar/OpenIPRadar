using System.Net;
using System.Net.Http;
using System.Text.Json;
using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Core.Configuration;
using OpenIPRadar.Core.Enums;
using OpenIPRadar.Core.Models;
using OpenIPRadar.Providers.Common;
using OpenIPRadar.Services.Http;

namespace OpenIPRadar.Providers.VirusTotal;

/// <summary>
/// Queries the VirusTotal v3 <c>/ip_addresses/{ip}</c> endpoint. Populates the VT Reputation,
/// VT Malicious, VT Suspicious, and Tags columns (and Country/ASN/ISP as fallbacks).
/// </summary>
public sealed class VirusTotalProvider : ProviderBase
{
    /// <summary>The stable provider key used in configuration and the key store.</summary>
    public const string ProviderName = "VirusTotal";

    /// <summary>Initializes the provider from application configuration.</summary>
    /// <param name="configuration">Provides this provider's settings.</param>
    /// <param name="keyStore">Secure API-key store.</param>
    /// <param name="rateLimiter">Rate limiter.</param>
    /// <param name="retryPolicy">Retry policy.</param>
    /// <param name="httpClient">Shared HTTP client.</param>
    /// <param name="logger">Application logger.</param>
    public VirusTotalProvider(
        IConfigurationService configuration,
        ISecureKeyStore keyStore,
        IRateLimiter rateLimiter,
        RetryPolicy retryPolicy,
        SharedHttpClient httpClient,
        IAppLogger logger)
        : base(configuration.GetProviderSettings(ProviderName) ?? new ProviderSettings(),
               keyStore, rateLimiter, retryPolicy, httpClient, logger)
    {
    }

    /// <inheritdoc />
    public override string Name => ProviderName;

    /// <inheritdoc />
    protected override string DisplayName => "VirusTotal";

    /// <inheritdoc />
    protected override HttpRequestMessage BuildCheckRequest(IpAddressEntry ip, string apiKey) =>
        CreateRequest($"{Settings.BaseUrl}{Settings.IpLookupPath}{Uri.EscapeDataString(ip.Address)}", apiKey);

    /// <inheritdoc />
    protected override HttpRequestMessage BuildConnectivityRequest(string apiKey) =>
        CreateRequest($"{Settings.BaseUrl}{Settings.IpLookupPath}8.8.8.8", apiKey);

    private static HttpRequestMessage CreateRequest(string url, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("x-apikey", apiKey);
        return request;
    }

    /// <inheritdoc />
    protected override ProviderResult ParseResponse(IpAddressEntry ip, HttpStatusCode statusCode, string body)
    {
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("attributes", out var attr))
        {
            return ProviderResult.Failure(Name, ProviderResultStatus.Error, "Missing 'data.attributes' in VirusTotal response.");
        }

        int? malicious = null;
        int? suspicious = null;
        if (attr.TryGetProperty("last_analysis_stats", out var stats))
        {
            malicious = stats.TryGetInt("malicious");
            suspicious = stats.TryGetInt("suspicious");
        }

        return new ProviderResult
        {
            ProviderName = Name,
            Status = ProviderResultStatus.Success,
            VtReputation = attr.TryGetInt("reputation"),
            VtMalicious = malicious,
            VtSuspicious = suspicious,
            Country = attr.TryGetString("country"),
            Asn = attr.TryGetInt("asn")?.ToString(),
            Isp = attr.TryGetString("as_owner"),
            Tags = attr.TryGetStringList("tags")
        };
    }
}
