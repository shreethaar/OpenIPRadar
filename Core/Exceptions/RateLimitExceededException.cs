namespace OpenIPRadar.Core.Exceptions;

/// <summary>
/// Raised when a provider rejects a request because its rate limit or quota was exceeded.
/// </summary>
public sealed class RateLimitExceededException : ProviderException
{
    /// <summary>The suggested delay before retrying, if the provider supplied one.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>Initializes a new instance of the <see cref="RateLimitExceededException"/> class.</summary>
    /// <param name="providerName">The provider name.</param>
    /// <param name="retryAfter">The suggested retry delay, if known.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public RateLimitExceededException(string providerName, TimeSpan? retryAfter = null, Exception? innerException = null)
        : base(providerName, $"Rate limit exceeded for provider '{providerName}'.", innerException)
    {
        RetryAfter = retryAfter;
    }
}
