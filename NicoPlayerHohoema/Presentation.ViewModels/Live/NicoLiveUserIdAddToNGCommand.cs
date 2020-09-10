using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.UseCase.NicoVideoPlayer;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace Hohoema.Presentation.ViewModels.Live
{
    public sealed class NicoLiveUserIdAddToNGCommand : DelegateCommandBase
    {
        private readonly CommentFiltering _commentFiltering;
        private readonly NicoVideoOwnerCacheRepository _nicoVideoOwnerRepository;

        public NicoLiveUserIdAddToNGCommand(CommentFiltering playerSettings, NicoVideoOwnerCacheRepository nicoVideoOwnerRepository)
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
