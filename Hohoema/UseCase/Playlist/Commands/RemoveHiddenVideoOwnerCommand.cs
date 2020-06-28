using Hohoema.Models;
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
        private readonly NGSettings _ngSettings;

        public RemoveHiddenVideoOwnerCommand(NGSettings ngSettings)
        {
            _ngSettings = ngSettings;
        }

        protected override bool CanExecute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent video)
            {
                if (video.ProviderId != null)
                {
                    return _ngSettings.IsNgVideoOwnerId(video.ProviderId) != null;
                }
            }

            return false;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent video)
            {
                if (video.ProviderId != null)
                {
                    _ngSettings.RemoveNGVideoOwnerId(video.ProviderId);
                }
            }
        }
    }
}
