namespace OpenIPRadar.Core.Models;

/// <summary>
/// A presentation-agnostic model of a completed scan, consumed by the HTML and PDF exporters.
/// Built once by the report-model builder and shared by every exporter so that switching or
/// adding an export format never touches the underlying data shape.
/// </summary>
/// <param name="GeneratedAtUtc">When the report was generated.</param>
/// <param name="Reports">The per-IP results included in the report.</param>
/// <param name="TotalScanned">Total number of addresses scanned.</param>
/// <param name="MaliciousCount">Number of addresses classified as malicious.</param>
/// <param name="SuspiciousCount">Number of addresses classified as suspicious.</param>
/// <param name="CleanCount">Number of addresses classified as clean.</param>
public sealed record ScanReportModel(
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<ThreatReport> Reports,
    int TotalScanned,
    int MaliciousCount,
    int SuspiciousCount,
    int CleanCount);
