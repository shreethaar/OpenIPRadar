using System.Net;
using System.Net.Http;
using System.Text.Json;
using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Core.Configuration;
using OpenIPRadar.Core.Enums;
using OpenIPRadar.Core.Exceptions;
using OpenIPRadar.Core.Models;
using OpenIPRadar.Services.Http;

namespace OpenIPRadar.Providers;

/// <summary>
/// Base class for all threat-intelligence providers. It is the single choke point that applies
/// the rate limiter, sends the HTTP request through the shared client, retries transient
/// failures, and maps every failure onto a <see cref="ProviderResult"/> — so a provider error
/// (network, timeout, auth, rate limit, malformed JSON) never throws out to the scan and never
/// crashes the application. Concrete providers implement only their request shape and parsing.
/// </summary>
public abstract class ProviderBase : IThreatProvider
{
    private readonly ISecureKeyStore _keyStore;
    private readonly IRateLimiter _rateLimiter;
    private readonly RetryPolicy _retryPolicy;
    private readonly SharedHttpClient _httpClient;

    /// <summary>Logger for provider diagnostics (never receives API keys).</summary>
    protected IAppLogger Logger { get; }

    /// <summary>The configured settings for this provider.</summary>
    protected ProviderSettings Settings { get; }

    /// <summary>Initializes the shared provider infrastructure.</summary>
    /// <param name="settings">This provider's configuration.</param>
    /// <param name="keyStore">Secure store for the provider's API key.</param>
    /// <param name="rateLimiter">Per-provider rate limiter.</param>
    /// <param name="retryPolicy">Transient-failure retry policy.</param>
    /// <param name="httpClient">The shared HTTP client.</param>
    /// <param name="logger">Application logger.</param>
    protected ProviderBase(
        ProviderSettings settings,
        ISecureKeyStore keyStore,
        IRateLimiter rateLimiter,
        RetryPolicy retryPolicy,
        SharedHttpClient httpClient,
        IAppLogger logger)
    {
        Settings = settings;
        _keyStore = keyStore;
        _rateLimiter = rateLimiter;
        _retryPolicy = retryPolicy;
        _httpClient = httpClient;
        Logger = logger;
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <summary>The human-friendly provider name shown in the UI.</summary>
    protected abstract string DisplayName { get; }

    /// <inheritdoc />
    public ProviderMetadata Metadata =>
        new(Name, DisplayName, Settings.RequestsPerMinute, Settings.Enabled);

    /// <inheritdoc />
    public bool IsConfigured => _keyStore.HasKey(Name);

    /// <inheritdoc />
    public async Task<ProviderResult> CheckAsync(IpAddressEntry ip, CancellationToken cancellationToken)
    {
        var apiKey = _keyStore.GetKey(Name);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return ProviderResult.Failure(Name, ProviderResultStatus.NotConfigured, "No API key configured.");
        }

        try
        {
            await _rateLimiter.WaitAsync(Name, cancellationToken).ConfigureAwait(false);

            return await _retryPolicy.ExecuteAsync(
                async ct => await SendAndParseAsync(ip, apiKey, ct).ConfigureAwait(false),
                IsTransient,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RateLimitExceededException ex)
        {
            Logger.Warning($"[{Name}] Rate limited while checking {ip.Address}.");
            return ProviderResult.Failure(Name, ProviderResultStatus.RateLimited, ex.Message);
        }
        catch (ApiAuthenticationException ex)
        {
            Logger.Error($"[{Name}] Authentication failed while checking {ip.Address}.", ex);
            return ProviderResult.Failure(Name, ProviderResultStatus.Error, ex.Message);
        }
        catch (Exception ex)
        {
            Logger.Error($"[{Name}] Error while checking {ip.Address}.", ex);
            return ProviderResult.Failure(Name, ProviderResultStatus.Error, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectivityAsync(CancellationToken cancellationToken)
    {
        var apiKey = _keyStore.GetKey(Name);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        try
        {
            await _rateLimiter.WaitAsync(Name, cancellationToken).ConfigureAwait(false);
            using var request = BuildConnectivityRequest(apiKey);
            using var response = await _httpClient.Client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Warning($"[{Name}] Connectivity test failed: {ex.Message}");
            return false;
        }
    }

    private async Task<ProviderResult> SendAndParseAsync(IpAddressEntry ip, string apiKey, CancellationToken ct)
    {
        using var request = BuildCheckRequest(ip, apiKey);
        using var response = await _httpClient.Client.SendAsync(request, ct).ConfigureAwait(false);

        switch ((int)response.StatusCode)
        {
            case 429:
                throw new RateLimitExceededException(Name, response.Headers.RetryAfter?.Delta);
            case 401:
            case 403:
                throw new ApiAuthenticationException(Name);
        }

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode && !CanHandleStatus(response.StatusCode))
        {
            // 5xx is transient (throw HttpRequestException so the retry policy engages);
            // other 4xx are treated as non-retryable provider errors.
            var message = $"HTTP {(int)response.StatusCode} from {Name}.";
            if ((int)response.StatusCode >= 500)
            {
                throw new HttpRequestException(message);
            }

            throw new ProviderException(Name, message);
        }

        try
        {
            return ParseResponse(ip, response.StatusCode, body);
        }
        catch (JsonException ex)
        {
            throw new ProviderException(Name, $"Malformed response from {Name}.", ex);
        }
    }

    /// <summary>Builds the request used to check a single IP address.</summary>
    /// <param name="ip">The address to check.</param>
    /// <param name="apiKey">The provider API key.</param>
    /// <returns>The configured HTTP request.</returns>
    protected abstract HttpRequestMessage BuildCheckRequest(IpAddressEntry ip, string apiKey);

    /// <summary>Builds the request used to verify connectivity and key validity.</summary>
    /// <param name="apiKey">The provider API key.</param>
    /// <returns>The configured HTTP request.</returns>
    protected abstract HttpRequestMessage BuildConnectivityRequest(string apiKey);

    /// <summary>Parses a (possibly non-success but handled) response body into a result.</summary>
    /// <param name="ip">The address that was checked.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="body">The response body text.</param>
    /// <returns>The parsed provider result.</returns>
    protected abstract ProviderResult ParseResponse(IpAddressEntry ip, HttpStatusCode statusCode, string body);

    /// <summary>
    /// Allows a provider to opt into handling a specific non-success status code (e.g. Shodan's
    /// 404 "no information available"). Defaults to <c>false</c>.
    /// </summary>
    /// <param name="statusCode">The non-success status code.</param>
    /// <returns><c>true</c> if the provider will parse this status itself.</returns>
    protected virtual bool CanHandleStatus(HttpStatusCode statusCode) => false;

    private static bool IsTransient(Exception ex) =>
        ex is HttpRequestException
        || ex is RateLimitExceededException
        || (ex is TaskCanceledException tce && tce.InnerException is TimeoutException);
}
