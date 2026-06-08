using System.Windows.Input;

namespace KeyboardCleanerPro.ViewModels;

/// <summary>
/// Minimalist relay command implementation that binds a delegate pair to <see cref="ICommand"/>.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?>  _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute    = execute    ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute()) { }

    public event EventHandler? CanExecuteChanged
    {
        add    => System.Windows.Input.CommandManager.RequerySuggested += value;
        remove => System.Windows.Input.CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter)    => _execute(parameter);

    /// <summary>Forces WPF command infrastructure to re-evaluate CanExecute.</summary>
    public static void RaiseCanExecuteChanged() =>
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
}
