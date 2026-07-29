using OpenIPRadar.Core.Models;

namespace OpenIPRadar.Services.Reporting;

/// <summary>
/// Shared helpers that render <see cref="ThreatReport"/> column values into display strings,
/// so the HTML and PDF exporters format the 15 columns identically. Missing values render as "N/A".
/// </summary>
internal static class ReportFormatting
{
    /// <summary>The ordered column headers, matching <see cref="RowValues"/>.</summary>
    public static readonly string[] Headers =
    {
        "IP", "Country", "ASN", "ISP", "Hostname", "Abuse Confidence", "Reports",
        "Last Reported", "VT Reputation", "VT Malicious", "VT Suspicious", "Open Ports",
        "Operating System", "Tags", "Threat Score"
    };

    private const string NotAvailable = "N/A";

    /// <summary>Returns the 15 display values for a report row, in header order.</summary>
    /// <param name="report">The report to render.</param>
    /// <returns>An array of formatted strings aligned to <see cref="Headers"/>.</returns>
    public static string[] RowValues(ThreatReport report) => new[]
    {
        report.IpAddress,
        Text(report.Country),
        Text(report.Asn),
        Text(report.Isp),
        Text(report.Hostname),
        Number(report.AbuseConfidence),
        Number(report.Reports),
        Date(report.LastReported),
        Number(report.VtReputation),
        Number(report.VtMalicious),
        Number(report.VtSuspicious),
        List(report.OpenPorts),
        Text(report.OperatingSystem),
        List(report.Tags),
        report.ThreatScore.ToString()
    };

    private static string Text(string? value) => string.IsNullOrWhiteSpace(value) ? NotAvailable : value;

    private static string Number(int? value) => value?.ToString() ?? NotAvailable;

    private static string Date(DateTimeOffset? value) =>
        value?.ToString("yyyy-MM-dd HH:mm 'UTC'") ?? NotAvailable;

    private static string List(IReadOnlyList<int>? values) =>
        values is { Count: > 0 } ? string.Join(", ", values) : NotAvailable;

    private static string List(IReadOnlyList<string>? values) =>
        values is { Count: > 0 } ? string.Join(", ", values) : NotAvailable;
}
