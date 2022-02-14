using Hohoema.Models.UseCase.Niconico.Player.Comment;
using Prism.Commands;

namespace Hohoema.Presentation.ViewModels.Niconico.Live
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
