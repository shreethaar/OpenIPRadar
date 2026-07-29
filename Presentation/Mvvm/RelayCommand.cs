using System.Windows.Input;

namespace OpenIPRadar.Presentation.Mvvm;

/// <summary>
/// A synchronous <see cref="ICommand"/> that delegates execution and can-execute logic to
/// supplied delegates.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>Initializes the command.</summary>
    /// <param name="execute">The action to run.</param>
    /// <param name="canExecute">An optional guard predicate.</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute();
}
