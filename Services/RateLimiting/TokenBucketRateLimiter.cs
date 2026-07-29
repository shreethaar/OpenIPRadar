using System.Collections.Concurrent;
using OpenIPRadar.Core.Abstractions;

namespace OpenIPRadar.Services.RateLimiting;

/// <summary>
/// Enforces a per-provider request budget by spacing requests at a minimum interval derived
/// from each provider's <c>RequestsPerMinute</c>. A per-provider mutex serializes access so
/// concurrent scan workers cannot burst past the configured limit. This directly protects the
/// tightest free-tier limits (e.g. VirusTotal at 4 requests/minute).
/// </summary>
public sealed class TokenBucketRateLimiter : IRateLimiter
{
    private readonly IConfigurationService _configuration;
    private readonly ConcurrentDictionary<string, ProviderGate> _gates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Initializes the limiter.</summary>
    /// <param name="configuration">Configuration used to read each provider's request budget.</param>
    public TokenBucketRateLimiter(IConfigurationService configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task WaitAsync(string providerName, CancellationToken cancellationToken)
    {
        var gate = _gates.GetOrAdd(providerName, CreateGate);

        await gate.Mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var earliest = gate.LastRequestUtc + gate.MinInterval;
            if (earliest > now)
            {
                await Task.Delay(earliest - now, cancellationToken).ConfigureAwait(false);
            }

            gate.LastRequestUtc = DateTimeOffset.UtcNow;
        }
        finally
        {
            gate.Mutex.Release();
        }
    }

    private ProviderGate CreateGate(string providerName)
    {
        var rpm = _configuration.GetProviderSettings(providerName)?.RequestsPerMinute ?? 60;
        rpm = Math.Max(1, rpm);
        return new ProviderGate(TimeSpan.FromMilliseconds(60_000d / rpm));
    }

    /// <summary>Per-provider throttling state.</summary>
    private sealed class ProviderGate
    {
        public ProviderGate(TimeSpan minInterval) => MinInterval = minInterval;

        public SemaphoreSlim Mutex { get; } = new(1, 1);

        public TimeSpan MinInterval { get; }

        public DateTimeOffset LastRequestUtc { get; set; } = DateTimeOffset.MinValue;
    }
}
