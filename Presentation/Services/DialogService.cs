using System.Windows;
using Microsoft.Win32;
using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Core.Enums;

namespace OpenIPRadar.Presentation.Services;

/// <summary>
/// WPF implementation of <see cref="IDialogService"/>. This is the only place standard dialogs
/// are invoked, keeping view models free of direct UI dependencies.
/// </summary>
public sealed class DialogService : IDialogService
{
    /// <inheritdoc />
    public string? OpenInputFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select an IP list",
            Filter = "IP lists (*.txt;*.csv)|*.txt;*.csv|Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            CheckFileExists = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public string? SaveReportFile(ReportFormat format)
    {
        var (filter, ext) = format switch
        {
            ReportFormat.Pdf => ("PDF report (*.pdf)|*.pdf", "pdf"),
            _ => ("HTML report (*.html)|*.html", "html")
        };

        var dialog = new SaveFileDialog
        {
            Title = "Export report",
            Filter = filter,
            DefaultExt = ext,
            FileName = $"OpenIPRadar-Report-{DateTime.Now:yyyyMMdd-HHmmss}.{ext}"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public void ShowMessage(string title, string message, bool isError = false)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            isError ? MessageBoxImage.Error : MessageBoxImage.Information);
    }
}
