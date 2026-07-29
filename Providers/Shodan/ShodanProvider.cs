using System.Net;
using System.Net.Http;
using System.Text.Json;
using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Core.Configuration;
using OpenIPRadar.Core.Enums;
using OpenIPRadar.Core.Models;
using OpenIPRadar.Providers.Common;
using OpenIPRadar.Services.Http;

namespace OpenIPRadar.Providers.Shodan;

/// <summary>
/// Queries the Shodan <c>/shodan/host/{ip}</c> endpoint. Populates the Open Ports, Operating
/// System, ASN, ISP, Hostname, and Tags columns. Connectivity is verified via the free
/// <c>/api-info</c> endpoint, which does not consume a query credit. A 404 response means Shodan
/// has no information for the address and is treated as a successful, empty result rather than
/// an error.
/// </summary>
public sealed class ShodanProvider : ProviderBase
{
    /// <summary>The stable provider key used in configuration and the key store.</summary>
    public const string ProviderName = "Shodan";

    /// <summary>Initializes the provider from application configuration.</summary>
    /// <param name="configuration">Provides this provider's settings.</param>
    /// <param name="keyStore">Secure API-key store.</param>
    /// <param name="rateLimiter">Rate limiter.</param>
    /// <param name="retryPolicy">Retry policy.</param>
    /// <param name="httpClient">Shared HTTP client.</param>
    /// <param name="logger">Application logger.</param>
    public ShodanProvider(
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
    protected override string DisplayName => "Shodan";

    /// <inheritdoc />
    protected override HttpRequestMessage BuildCheckRequest(IpAddressEntry ip, string apiKey) =>
        new(HttpMethod.Get,
            $"{Settings.BaseUrl}{Settings.HostPath}{Uri.EscapeDataString(ip.Address)}?key={Uri.EscapeDataString(apiKey)}");

    /// <inheritdoc />
    protected override HttpRequestMessage BuildConnectivityRequest(string apiKey) =>
        new(HttpMethod.Get, $"{Settings.BaseUrl}{Settings.ApiInfoPath}?key={Uri.EscapeDataString(apiKey)}");

    /// <inheritdoc />
    protected override bool CanHandleStatus(HttpStatusCode statusCode) => statusCode == HttpStatusCode.NotFound;

    /// <inheritdoc />
    protected override ProviderResult ParseResponse(IpAddressEntry ip, HttpStatusCode statusCode, string body)
    {
        if (statusCode == HttpStatusCode.NotFound)
        {
            // No information available for this host — a valid, empty result.
            return new ProviderResult { ProviderName = Name, Status = ProviderResultStatus.Success };
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        return new ProviderResult
        {
            ProviderName = Name,
            Status = ProviderResultStatus.Success,
            OpenPorts = root.TryGetIntList("ports"),
            OperatingSystem = root.TryGetString("os"),
            Asn = root.TryGetString("asn"),
            Isp = root.TryGetString("org") ?? root.TryGetString("isp"),
            Hostnames = root.TryGetStringList("hostnames"),
            Country = root.TryGetString("country_name"),
            Tags = root.TryGetStringList("tags")
        };
    }
}
