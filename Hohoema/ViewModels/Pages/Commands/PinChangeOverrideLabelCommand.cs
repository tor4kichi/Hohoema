using I18NPortable;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.UseCase.Services;
using Hohoema.Models.Pages;

namespace Hohoema.UseCase.Pin
{
    public sealed class PinChangeOverrideLabelCommand : DelegateCommandBase
    {
        private readonly ITextInputDialogService _dialogService;

        public PinChangeOverrideLabelCommand(ITextInputDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is HohoemaPin;
        }

        protected override async void Execute(object parameter)
        {
            
        }
    }
}
