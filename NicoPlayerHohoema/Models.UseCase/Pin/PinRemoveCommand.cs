using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.PageNavigation;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Pin
{
    public sealed class PinRemoveCommand : DelegateCommandBase
    {
        private readonly PinSettings _pinSettings;

        public PinRemoveCommand(PinSettings pinSettings)
        {
            _pinSettings = pinSettings;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is HohoemaPin;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is HohoemaPin pin)
            {
                _pinSettings.DeleteItem(pin.Id);
            }
        }
    }
}
