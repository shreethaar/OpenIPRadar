using System.Windows.Input;

namespace OpenIPRadar.Presentation.Mvvm;

/// <summary>
/// An asynchronous <see cref="ICommand"/> that tracks its own execution state to prevent
/// re-entrancy and surfaces unhandled exceptions to an optional handler. Keeps async work off
/// the constructor of view models while never blocking the UI thread.
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private readonly Action<Exception>? _onError;
    private bool _isExecuting;

    /// <summary>Initializes the command.</summary>
    /// <param name="execute">The asynchronous action to run.</param>
    /// <param name="canExecute">An optional guard predicate.</param>
    /// <param name="onError">An optional handler for unhandled exceptions.</param>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, Action<Exception>? onError = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        _onError = onError;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>Whether the command is currently running.</summary>
    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            _isExecuting = value;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        IsExecuting = true;
        try
        {
            await _execute().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
        finally
        {
            IsExecuting = false;
        }
    }
}
