using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Core.Enums;
using OpenIPRadar.Core.Models;

namespace OpenIPRadar.Services.Reporting;

/// <summary>
/// Assembles a <see cref="ScanReportModel"/> — including risk-level summary counts — from the
/// per-IP scan results. The resulting model is format-agnostic and shared by all exporters.
/// </summary>
public sealed class ReportModelBuilder : IReportModelBuilder
{
    /// <inheritdoc />
    public ScanReportModel Build(IReadOnlyList<ThreatReport> reports)
    {
        var malicious = reports.Count(r => r.RiskLevel == RiskLevel.Malicious);
        var suspicious = reports.Count(r => r.RiskLevel == RiskLevel.Suspicious);
        var clean = reports.Count(r => r.RiskLevel == RiskLevel.Clean);

        return new ScanReportModel(
            DateTimeOffset.UtcNow,
            reports,
            reports.Count,
            malicious,
            suspicious,
            clean);
    }
}
