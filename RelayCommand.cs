using System.Windows.Input;

namespace Decision_Trees_Visualizer;
public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Func<object, bool> _canExecute;

    public RelayCommand(Action execute, Func<object, bool> canExecute = null)
    {
        _execute = param => execute();
        _canExecute = canExecute;
    }

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

    public void Execute(object parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

