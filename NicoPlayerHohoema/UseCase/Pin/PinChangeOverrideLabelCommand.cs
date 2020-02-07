using I18NPortable;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Pin
{
    public sealed class PinChangeOverrideLabelCommand : DelegateCommandBase
    {
        private readonly DialogService _dialogService;

        public PinChangeOverrideLabelCommand(DialogService dialogService)
        {
            _dialogService = dialogService;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is HohoemaPin;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is HohoemaPin pin)
            {
                var name = pin.OverrideLabel ?? $"{pin.Label} ({pin.PageType.Translate()})";
                var result = await _dialogService.GetTextAsync(
                    $"RenameX".Translate(name),
                    "PinRenameDialogPlacefolder_EmptyToDefault".Translate(),
                    name,
                    (s) => true
                    );

                pin.OverrideLabel = string.IsNullOrEmpty(result) ? null : result;
            }
        }
    }
}
