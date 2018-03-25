using NicoPlayerHohoema.Models;
using Prism.Commands;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.Commands
{
    public sealed class NicoLiveUserIdRemoveFromNGCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is string;
        }

        protected override void Execute(object parameter)
        {
            var userSettings = App.Current.Container.Resolve<HohoemaUserSettings>();

            userSettings.NGSettings.RemoveNGLiveCommentUserId(parameter as string);
        }
    }
}
