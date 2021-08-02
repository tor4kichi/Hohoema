using Hohoema.Models.UseCase.Niconico.Player;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Player
{
    public sealed class ShowPrimaryViewCommand : DelegateCommandBase
    {
        private readonly SecondaryViewPlayerManager _scondaryViewPlayerManager;

        public ShowPrimaryViewCommand(SecondaryViewPlayerManager scondaryViewPlayerManager)
        {
            _scondaryViewPlayerManager = scondaryViewPlayerManager;
        }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            _ = _scondaryViewPlayerManager.ShowMainViewAsync();
        }
    }
}
