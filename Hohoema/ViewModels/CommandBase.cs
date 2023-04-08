using System;
using System.Windows.Input;

namespace Hohoema.ViewModels;

public abstract class CommandBase : ICommand
{
    public event EventHandler CanExecuteChanged;

    protected abstract bool CanExecute(object parameter);
    protected abstract void Execute(object parameter);

    bool ICommand.CanExecute(object parameter)
    {
        return CanExecute(parameter);
    }

    void ICommand.Execute(object parameter)
    {
        Execute(parameter);
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
