using System.Net.Http;
using OpenIPRadar.Core.Configuration;

namespace OpenIPRadar.Services.Http;

/// <summary>
/// Owns a single, application-wide <see cref="HttpClient"/> built on a
/// <see cref="SocketsHttpHandler"/> with a bounded pooled-connection lifetime. Sharing one
/// client avoids socket exhaustion (replacing the role of <c>IHttpClientFactory</c>) while the
/// connection lifetime keeps DNS resolution fresh. Providers apply their own base address and
/// headers per request rather than mutating this shared instance.
/// </summary>
public sealed class SharedHttpClient : IDisposable
{
    private readonly SocketsHttpHandler _handler;

    /// <summary>The shared client instance.</summary>
    public HttpClient Client { get; }

    /// <summary>Initializes the shared client using the configured timeout.</summary>
    /// <param name="settings">Scan settings supplying the HTTP timeout.</param>
    public SharedHttpClient(ScanningSettings settings)
    {
        _handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            AutomaticDecompression = System.Net.DecompressionMethods.All
        };

        Client = new HttpClient(_handler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(5, settings.HttpTimeoutSeconds))
        };
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("OpenIPRadar/1.0");
    }

    /// <summary>Disposes the underlying client and handler.</summary>
    public void Dispose()
    {
        Client.Dispose();
        _handler.Dispose();
    }
}
