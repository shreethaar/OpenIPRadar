using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Core.Enums;
using OpenIPRadar.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OpenIPRadar.Services.Reporting;

/// <summary>
/// Renders a scan into a PDF using QuestPDF — the single external dependency in the project,
/// fully confined to this class behind <see cref="IReportExporter"/>. QuestPDF generation is
/// synchronous, so it is offloaded to a background thread to keep the UI responsive.
/// </summary>
public sealed class PdfReportExporter : IReportExporter
{
    static PdfReportExporter()
    {
        // QuestPDF Community license (free for this project's scale).
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public ReportFormat Format => ReportFormat.Pdf;

    /// <inheritdoc />
    public async Task ExportAsync(ScanReportModel model, string outputPath, CancellationToken cancellationToken)
    {
        await Task.Run(() => BuildDocument(model).GeneratePdf(outputPath), cancellationToken).ConfigureAwait(false);
    }

    private static Document BuildDocument(ScanReportModel model) => Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(18);
            page.DefaultTextStyle(t => t.FontSize(7));

            page.Header().Column(header =>
            {
                header.Item().Text("OpenIPRadar Scan Report").FontSize(16).SemiBold();
                header.Item().Text(
                    $"Generated {model.GeneratedAtUtc:yyyy-MM-dd HH:mm 'UTC'}  |  {model.TotalScanned} address(es)  |  " +
                    $"Malicious: {model.MaliciousCount}   Suspicious: {model.SuspiciousCount}   Clean: {model.CleanCount}")
                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                header.Item().PaddingTop(6);
            });

            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var _ in ReportFormatting.Headers)
                    {
                        columns.RelativeColumn();
                    }
                });

                foreach (var head in ReportFormatting.Headers)
                {
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text(head).SemiBold();
                }

                foreach (var report in model.Reports)
                {
                    var background = report.RiskLevel switch
                    {
                        RiskLevel.Malicious => Colors.Red.Lighten4,
                        RiskLevel.Suspicious => Colors.Yellow.Lighten4,
                        _ => Colors.White
                    };

                    foreach (var value in ReportFormatting.RowValues(report))
                    {
                        table.Cell().Background(background).BorderBottom(0.5f)
                            .BorderColor(Colors.Grey.Lighten2).Padding(3).Text(value);
                    }
                }
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        });
    });
}
