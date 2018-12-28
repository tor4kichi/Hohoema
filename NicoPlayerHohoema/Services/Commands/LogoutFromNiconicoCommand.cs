using NicoPlayerHohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Commands
{
    public sealed class LogoutFromNiconicoCommand : DelegateCommandBase
    {
        public LogoutFromNiconicoCommand(
            NiconicoSession niconicoSession
            )
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
            await NiconicoSession.SignOut();
        }
    }
}
