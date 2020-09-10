using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.Services.Player
{
    public sealed class ShowPrimaryViewCommand : DelegateCommandBase
    {
        private readonly ScondaryViewPlayerManager _scondaryViewPlayerManager;

        public ShowPrimaryViewCommand(ScondaryViewPlayerManager scondaryViewPlayerManager)
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
