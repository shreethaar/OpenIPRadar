namespace OpenIPRadar.Core.Exceptions;

/// <summary>
/// Raised when a provider rejects a request because the API key is missing or invalid.
/// </summary>
public sealed class ApiAuthenticationException : ProviderException
{
    /// <summary>Initializes a new instance of the <see cref="ApiAuthenticationException"/> class.</summary>
    /// <param name="providerName">The provider name.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public ApiAuthenticationException(string providerName, Exception? innerException = null)
        : base(providerName, $"Authentication failed for provider '{providerName}'. Check the API key.", innerException)
    {
    }
}
