using NicoPlayerHohoema.Models;
using Prism.Commands;
using Unity;

namespace NicoPlayerHohoema.Commands
{
    public sealed class NicoLiveUserIdRemoveFromNGCommand : DelegateCommandBase
    {
        public NicoLiveUserIdRemoveFromNGCommand(NGSettings ngSettings)
        {
            NgSettings = ngSettings;
        }

        public NGSettings NgSettings { get; }

        protected override bool CanExecute(object parameter)
        {
            return parameter is string;
        }

        protected override void Execute(object parameter)
        {
            NgSettings.RemoveNGLiveCommentUserId(parameter as string);
        }
    }
}
