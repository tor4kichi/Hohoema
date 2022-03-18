using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Account
{
    public sealed class LogoutFromNiconicoCommand : CommandBase
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
