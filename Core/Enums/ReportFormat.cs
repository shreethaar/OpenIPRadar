namespace OpenIPRadar.Core.Enums;

/// <summary>
/// Identifies an export format for a scan report.
/// </summary>
public enum ReportFormat
{
    /// <summary>A self-contained HTML document (generated natively).</summary>
    Html,

    /// <summary>A PDF document (generated via QuestPDF).</summary>
    Pdf
}
