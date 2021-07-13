using Hohoema.Models.UseCase.Niconico.Player.Events;
using Microsoft.Toolkit.Mvvm.Messaging;
using Prism.Commands;

namespace Hohoema.Presentation.ViewModels.Player
{
    public sealed class TogglePlayerDisplayViewCommand : DelegateCommandBase
    {
        private readonly IMessenger _messenger;

        public TogglePlayerDisplayViewCommand(IMessenger messenger)
        {
            _messenger = messenger;
        }
        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            _messenger.Send(new ChangePlayerDisplayViewRequestMessage());
        }
    }
}
