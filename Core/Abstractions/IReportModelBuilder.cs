using OpenIPRadar.Core.Models;

namespace OpenIPRadar.Core.Abstractions;

/// <summary>
/// Builds a <see cref="ScanReportModel"/> (with summary statistics) from a set of scan results.
/// </summary>
public interface IReportModelBuilder
{
    /// <summary>Aggregates the given reports into a shareable report model.</summary>
    /// <param name="reports">The per-IP scan results.</param>
    /// <returns>The assembled report model.</returns>
    ScanReportModel Build(IReadOnlyList<ThreatReport> reports);
}
