namespace OpenIPRadar.Core.Abstractions;

/// <summary>
/// Reads the raw textual content of supported input files (TXT, CSV) for IP extraction.
/// </summary>
public interface IInputFileReader
{
    /// <summary>Determines whether the file at the given path has a supported extension.</summary>
    /// <param name="filePath">The path to test.</param>
    /// <returns><c>true</c> if the file type is supported; otherwise <c>false</c>.</returns>
    bool IsSupported(string filePath);

    /// <summary>Reads the file and returns its raw text content.</summary>
    /// <param name="filePath">The path to read.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The file's text content.</returns>
    Task<string> ReadAsync(string filePath, CancellationToken cancellationToken);
}
