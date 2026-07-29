using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace OpenIPRadar;

/// <summary>
/// Application entry point. Builds the composition root, installs global exception handlers so
/// unhandled failures are logged and reported gracefully rather than crashing, and shows the
/// main window.
/// </summary>
public partial class App : Application
{
    private CompositionRoot? _composition;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _composition = new CompositionRoot();

        var window = new MainWindow(_composition.MainViewModel);
        window.Show();
    }

    /// <inheritdoc />
    protected override async void OnExit(ExitEventArgs e)
    {
        if (_composition is not null)
        {
            await _composition.DisposeAsync();
        }

        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}",
            "OpenIPRadar",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        // Keep the application alive; the error has been surfaced to the user.
        e.Handled = true;
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Prevent process termination from an unobserved background task exception.
        e.SetObserved();
    }
}
