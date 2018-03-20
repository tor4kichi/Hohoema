using NicoPlayerHohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.Commands
{
    public sealed class NicoLiveUserIdAddToNGCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is string;
        }

        protected override void Execute(object parameter)
        {
            var userSettings = App.Current.Container.Resolve<HohoemaUserSettings>();

            userSettings.NGSettings.AddNGUserId(parameter as string);
        }
    }
}
