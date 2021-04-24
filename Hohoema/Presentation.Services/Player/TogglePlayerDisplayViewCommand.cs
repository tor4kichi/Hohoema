using Microsoft.Toolkit.Mvvm.Messaging;
using Prism.Commands;

namespace Hohoema.Presentation.Services.Player
{
    public sealed class TogglePlayerDisplayViewCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            StrongReferenceMessenger.Default.Send(new ChangePlayerDisplayViewRequestMessage());
        }
    }
}
