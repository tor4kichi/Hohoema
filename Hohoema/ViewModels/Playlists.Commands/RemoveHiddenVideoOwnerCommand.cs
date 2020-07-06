using Hohoema.Models;
using Hohoema.Models.Repository;
using Hohoema.Models.Repository.App;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Playlist.Commands
{
    public sealed class RemoveHiddenVideoOwnerCommand : DelegateCommandBase
    {
        private readonly VideoListFilterSettings _ngSettings;

        public RemoveHiddenVideoOwnerCommand(VideoListFilterSettings ngSettings)
        {
            _ngSettings = ngSettings;
        }

        protected override bool CanExecute(object parameter)
        {
            if (parameter is IVideoContent video)
            {
                if (video.ProviderId != null)
                {
                    return _ngSettings.IsNgVideoOwner(video.ProviderId);
                }
            }

            return false;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is IVideoContent video)
            {
                if (video.ProviderId != null)
                {
                    _ngSettings.RemoveNgVideoOwner(video.ProviderId);
                }
            }
        }
    }
}
