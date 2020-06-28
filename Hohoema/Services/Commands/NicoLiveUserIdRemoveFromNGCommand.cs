using Hohoema.Models;
using Prism.Commands;
using Unity;

namespace Hohoema.Commands
{
    public sealed class NicoLiveUserIdRemoveFromNGCommand : DelegateCommandBase
    {
        private readonly PlayerSettings _playerSettings;

        public NicoLiveUserIdRemoveFromNGCommand(PlayerSettings playerSettings)
        {
            _playerSettings = playerSettings;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is string;
        }

        protected override void Execute(object parameter)
        {
            _playerSettings.RemoveNGLiveCommentUserId(parameter as string);
        }
    }
}
