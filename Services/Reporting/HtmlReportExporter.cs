using System.IO;
using System.Net;
using System.Text;
using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Core.Enums;
using OpenIPRadar.Core.Models;

namespace OpenIPRadar.Services.Reporting;

/// <summary>
/// Renders a scan into a self-contained HTML document with inline CSS (no external assets),
/// using only the BCL. All dynamic text is HTML-encoded to prevent markup injection.
/// </summary>
public sealed class HtmlReportExporter : IReportExporter
{
    /// <inheritdoc />
    public ReportFormat Format => ReportFormat.Html;

    /// <inheritdoc />
    public async Task ExportAsync(ScanReportModel model, string outputPath, CancellationToken cancellationToken)
    {
        var html = BuildHtml(model);
        await File.WriteAllTextAsync(outputPath, html, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildHtml(ScanReportModel model)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.Append("<title>OpenIPRadar Report</title><style>");
        sb.Append("body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#1a1a1a;}");
        sb.Append("h1{margin:0 0 4px;}.meta{color:#666;margin-bottom:16px;}");
        sb.Append(".summary span{display:inline-block;padding:6px 12px;border-radius:4px;margin-right:8px;font-weight:600;}");
        sb.Append(".mal{background:#f8d7da;color:#842029;}.sus{background:#fff3cd;color:#664d03;}.cln{background:#d1e7dd;color:#0f5132;}");
        sb.Append("table{border-collapse:collapse;width:100%;font-size:12px;}");
        sb.Append("th,td{border:1px solid #ddd;padding:6px 8px;text-align:left;vertical-align:top;}");
        sb.Append("th{background:#f2f2f2;position:sticky;top:0;}");
        sb.Append("tr.row-mal{background:#fdecee;}tr.row-sus{background:#fffbea;}");
        sb.Append("</style></head><body>");

        sb.Append("<h1>OpenIPRadar Scan Report</h1>");
        sb.Append($"<div class=\"meta\">Generated {WebUtility.HtmlEncode(model.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm 'UTC'"))} &middot; {model.TotalScanned} address(es) scanned</div>");

        sb.Append("<div class=\"summary\">");
        sb.Append($"<span class=\"mal\">Malicious: {model.MaliciousCount}</span>");
        sb.Append($"<span class=\"sus\">Suspicious: {model.SuspiciousCount}</span>");
        sb.Append($"<span class=\"cln\">Clean: {model.CleanCount}</span>");
        sb.Append("</div><br>");

        sb.Append("<table><thead><tr>");
        foreach (var header in ReportFormatting.Headers)
        {
            sb.Append($"<th>{WebUtility.HtmlEncode(header)}</th>");
        }

        sb.Append("</tr></thead><tbody>");

        foreach (var report in model.Reports)
        {
            var rowClass = report.RiskLevel switch
            {
                RiskLevel.Malicious => " class=\"row-mal\"",
                RiskLevel.Suspicious => " class=\"row-sus\"",
                _ => string.Empty
            };

            sb.Append($"<tr{rowClass}>");
            foreach (var value in ReportFormatting.RowValues(report))
            {
                sb.Append($"<td>{WebUtility.HtmlEncode(value)}</td>");
            }

            sb.Append("</tr>");
        }

        sb.Append("</tbody></table></body></html>");
        return sb.ToString();
    }
}
