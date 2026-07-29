using OpenIPRadar.Core.Enums;

namespace OpenIPRadar.Core.Abstractions;

/// <summary>
/// Abstracts user-facing dialogs (file pickers, message boxes) so view models remain
/// free of direct WPF dependencies and stay testable.
/// </summary>
public interface IDialogService
{
    /// <summary>Prompts the user to select an input file (TXT/CSV).</summary>
    /// <returns>The selected path, or <c>null</c> if cancelled.</returns>
    string? OpenInputFile();

    /// <summary>Prompts the user for a destination path when exporting a report.</summary>
    /// <param name="format">The report format being exported.</param>
    /// <returns>The chosen path, or <c>null</c> if cancelled.</returns>
    string? SaveReportFile(ReportFormat format);

    /// <summary>Displays an informational or error message to the user.</summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The message body.</param>
    /// <param name="isError">Whether to present the message as an error.</param>
    void ShowMessage(string title, string message, bool isError = false);
}
