using NicoPlayerHohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Commands
{
    public sealed class LoginToNiconicoCommand : DelegateCommandBase
    {
        public LoginToNiconicoCommand(NiconicoSession niconicoSession)
        {
            NiconicoSession = niconicoSession;
        }

        public NiconicoSession NiconicoSession { get; }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override async void Execute(object parameter)
        {
            var loginDialog = new Dialogs.NiconicoLoginDialog(NiconicoSession);
            await loginDialog.ShowAsync();
        }
    }
}
