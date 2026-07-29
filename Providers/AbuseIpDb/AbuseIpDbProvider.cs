using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Core.Configuration;
using OpenIPRadar.Core.Enums;
using OpenIPRadar.Core.Models;
using OpenIPRadar.Providers.Common;
using OpenIPRadar.Services.Http;

namespace OpenIPRadar.Providers.AbuseIpDb;

/// <summary>
/// Queries the AbuseIPDB v2 <c>/check</c> endpoint. Populates the Abuse Confidence, Reports,
/// Last Reported, Country, ISP, and Hostname columns of a report.
/// </summary>
public sealed class AbuseIpDbProvider : ProviderBase
{
    /// <summary>The stable provider key used in configuration and the key store.</summary>
    public const string ProviderName = "AbuseIPDB";

    /// <summary>Initializes the provider from application configuration.</summary>
    /// <param name="configuration">Provides this provider's settings.</param>
    /// <param name="keyStore">Secure API-key store.</param>
    /// <param name="rateLimiter">Rate limiter.</param>
    /// <param name="retryPolicy">Retry policy.</param>
    /// <param name="httpClient">Shared HTTP client.</param>
    /// <param name="logger">Application logger.</param>
    public AbuseIpDbProvider(
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
    protected override string DisplayName => "AbuseIPDB";

    /// <inheritdoc />
    protected override HttpRequestMessage BuildCheckRequest(IpAddressEntry ip, string apiKey)
    {
        var url = $"{Settings.BaseUrl}{Settings.CheckPath}" +
                  $"?ipAddress={Uri.EscapeDataString(ip.Address)}" +
                  $"&maxAgeInDays={Settings.MaxAgeInDays}&verbose";
        return CreateRequest(url, apiKey);
    }

    /// <inheritdoc />
    protected override HttpRequestMessage BuildConnectivityRequest(string apiKey)
    {
        // A cheap, well-known lookup that validates the key without special quota concerns.
        var url = $"{Settings.BaseUrl}{Settings.CheckPath}?ipAddress=8.8.8.8&maxAgeInDays=1";
        return CreateRequest(url, apiKey);
    }

    private static HttpRequestMessage CreateRequest(string url, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Key", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    /// <inheritdoc />
    protected override ProviderResult ParseResponse(IpAddressEntry ip, HttpStatusCode statusCode, string body)
    {
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("data", out var data))
        {
            return ProviderResult.Failure(Name, ProviderResultStatus.Error, "Missing 'data' in AbuseIPDB response.");
        }

        string? country = data.TryGetString("countryName") ?? data.TryGetString("countryCode");

        return new ProviderResult
        {
            ProviderName = Name,
            Status = ProviderResultStatus.Success,
            Country = country,
            Isp = data.TryGetString("isp"),
            Hostnames = data.TryGetStringList("hostnames"),
            AbuseConfidenceScore = data.TryGetInt("abuseConfidenceScore"),
            TotalReports = data.TryGetInt("totalReports"),
            LastReportedAt = data.TryGetDateTimeOffset("lastReportedAt")
        };
    }
}
