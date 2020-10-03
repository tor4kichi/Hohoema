using Hohoema.Models.Domain;
using Hohoema.Models.UseCase.NicoVideos.Player;
using Prism.Commands;
using Unity;

namespace Hohoema.Presentation.ViewModels.Live
{
    public sealed class NicoLiveUserIdRemoveFromNGCommand : DelegateCommandBase
    {
        private readonly CommentFilteringFacade _playerSettings;

        public NicoLiveUserIdRemoveFromNGCommand(CommentFilteringFacade playerSettings)
        {
            _playerSettings = playerSettings;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is string;
        }

        protected override void Execute(object parameter)
        {
            _playerSettings.RemoveFilteringCommentOwnerId(parameter as string);
        }
    }
}
