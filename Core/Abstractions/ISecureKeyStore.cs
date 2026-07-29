namespace OpenIPRadar.Core.Abstractions;

/// <summary>
/// Stores and retrieves provider API keys, encrypted at rest using Windows DPAPI.
/// Keys are never written in plaintext and never logged.
/// </summary>
public interface ISecureKeyStore
{
    /// <summary>Retrieves the decrypted API key for a provider, or <c>null</c> if none is stored.</summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>The plaintext key, or <c>null</c>.</returns>
    string? GetKey(string providerName);

    /// <summary>Encrypts and persists an API key for a provider.</summary>
    /// <param name="providerName">The provider name.</param>
    /// <param name="apiKey">The plaintext key to store.</param>
    void SetKey(string providerName, string apiKey);

    /// <summary>Removes any stored key for a provider.</summary>
    /// <param name="providerName">The provider name.</param>
    void RemoveKey(string providerName);

    /// <summary>Indicates whether a key is stored for a provider.</summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns><c>true</c> if a key exists; otherwise <c>false</c>.</returns>
    bool HasKey(string providerName);
}
