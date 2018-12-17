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
            return parameter is HohoemaPin;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is HohoemaPin pin)
            {
                var pinSettings = App.Current.Container.Resolve<PinSettings>();
                
                if (pin != null)
                {
                    pinSettings.Pins.Remove(pin);
                    pinSettings.Save().ConfigureAwait(false);
                }
            }
        }
    }
}
