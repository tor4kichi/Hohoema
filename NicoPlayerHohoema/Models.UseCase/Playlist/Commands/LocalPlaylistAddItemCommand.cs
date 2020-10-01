
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist.Commands
{
    public sealed class LocalPlaylistAddItemCommand : VideoContentSelectionCommandBase
    {
        private readonly LocalPlaylist _playlist;

        public LocalPlaylistAddItemCommand(LocalPlaylist playlist)
        {
            _playlist = playlist;
        }

        protected override void Execute(IVideoContent content)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            _playlist.AddPlaylistItem(content);
        }

        protected override void Execute(IEnumerable<IVideoContent> items)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            _playlist.AddPlaylistItem(items);
        }
    }
}
