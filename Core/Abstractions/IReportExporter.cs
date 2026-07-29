using OpenIPRadar.Core.Enums;
using OpenIPRadar.Core.Models;

namespace OpenIPRadar.Core.Abstractions;

/// <summary>
/// Exports a completed scan to a specific file format. Each implementation handles exactly
/// one <see cref="ReportFormat"/>; the QuestPDF dependency is confined to the PDF implementation.
/// </summary>
public interface IReportExporter
{
    /// <summary>The format this exporter produces.</summary>
    ReportFormat Format { get; }

    /// <summary>
    /// Writes the report to the specified path.
    /// </summary>
    /// <param name="model">The presentation-agnostic report model.</param>
    /// <param name="outputPath">The destination file path.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task that completes when the file has been written.</returns>
    Task ExportAsync(ScanReportModel model, string outputPath, CancellationToken cancellationToken);
}
