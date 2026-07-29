using System.IO;
using OpenIPRadar.Core.Abstractions;

namespace OpenIPRadar.Services.Input;

/// <summary>
/// Reads the raw text of supported input files. Both TXT and CSV are read as plain text; the
/// <see cref="IIpExtractor"/> is responsible for locating IP addresses within that text,
/// regardless of column layout.
/// </summary>
public sealed class InputFileReader : IInputFileReader
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".txt", ".csv" };

    /// <inheritdoc />
    public bool IsSupported(string filePath) =>
        SupportedExtensions.Contains(Path.GetExtension(filePath));

    /// <inheritdoc />
    public async Task<string> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!IsSupported(filePath))
        {
            throw new NotSupportedException($"Unsupported input file type: {Path.GetExtension(filePath)}");
        }

        return await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
    }
}
