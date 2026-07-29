namespace OpenIPRadar.Core.Exceptions;

/// <summary>
/// The base exception for failures originating from a threat-intelligence provider.
/// These are caught at the provider boundary and converted into a failed result so a
/// single provider failure never crashes a scan.
/// </summary>
public class ProviderException : Exception
{
    /// <summary>The name of the provider that raised the exception.</summary>
    public string ProviderName { get; }

    /// <summary>Initializes a new instance of the <see cref="ProviderException"/> class.</summary>
    /// <param name="providerName">The provider name.</param>
    /// <param name="message">A description of the failure.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public ProviderException(string providerName, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ProviderName = providerName;
    }
}
