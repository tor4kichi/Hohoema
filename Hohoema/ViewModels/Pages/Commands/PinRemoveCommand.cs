using Hohoema.Models.Pages;
using Hohoema.Models.Repository.App;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Pin
{
    public sealed class PinRemoveCommand : DelegateCommandBase
    {
        private readonly PinRepository _pinRepository;

        public PinRemoveCommand(PinRepository pinRepository)
        {
            _pinRepository = pinRepository;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is HohoemaPin;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is HohoemaPin pin)
            {
                _pinRepository.DeleteItem(pin);
            }
        }
    }
}
