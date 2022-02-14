using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.UseCase.Niconico.Player.Comment;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Live
{
    public sealed class NicoLiveUserIdAddToNGCommand : DelegateCommandBase
    {
        private readonly CommentFilteringFacade _commentFiltering;
        private readonly NicoVideoOwnerCacheRepository _nicoVideoOwnerRepository;

        public NicoLiveUserIdAddToNGCommand(CommentFilteringFacade playerSettings, NicoVideoOwnerCacheRepository nicoVideoOwnerRepository)
        {
            _commentFiltering = playerSettings;
            _nicoVideoOwnerRepository = nicoVideoOwnerRepository;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is string;
        }

        protected override void Execute(object parameter)
        {
            var userId = parameter as string;
            var screenName = _nicoVideoOwnerRepository.Get(userId)?.ScreenName;

            _commentFiltering.AddFilteringCommentOwnerId(userId, screenName);
        }
    }
}
