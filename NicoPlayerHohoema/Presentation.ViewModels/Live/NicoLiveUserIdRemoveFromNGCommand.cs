using Hohoema.Models.Domain;
using Hohoema.Models.UseCase.NicoVideoPlayer;
using Prism.Commands;
using Unity;

namespace Hohoema.Presentation.ViewModels.Live
{
    public sealed class NicoLiveUserIdRemoveFromNGCommand : DelegateCommandBase
    {
        private readonly CommentFiltering _playerSettings;

        public NicoLiveUserIdRemoveFromNGCommand(CommentFiltering playerSettings)
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
