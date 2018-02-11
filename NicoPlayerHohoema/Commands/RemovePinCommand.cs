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
    public sealed class RemovePinCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is ViewModels.MenuItemViewModel;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is ViewModels.MenuItemViewModel)
            {
                var menuItem = parameter as ViewModels.MenuItemViewModel;
                var hohoemaApp = App.Current.Container.Resolve<HohoemaApp>();
                var pinSettings = hohoemaApp.UserSettings.PinSettings;
                var pin = pinSettings.Pins.FirstOrDefault(x => x.PageType == menuItem.PageType && x.Parameter == menuItem.Parameter);
                if (pin != null)
                {
                    pinSettings.Pins.Remove(pin);
                    pinSettings.Save().ConfigureAwait(false);
                }
            }
        }
    }
}
