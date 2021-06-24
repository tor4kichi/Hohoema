
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase.Playlist;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class PlayWithPlaylistCommand : VideoContentSelectionCommandBase
    {
        private readonly IPlaylist _playlist;
        private readonly IMessenger _messenger;

        public PlayWithPlaylistCommand(IPlaylist playlist, IMessenger messenger)
        {
            _playlist = playlist;
            _messenger = messenger;
        }

        protected override void Execute(IVideoContent content)
        {
            _messenger.Send(new VideoPlayRequestMessage() { VideoId = content.VideoId, PlaylistId = _playlist.PlaylistId.Id, PlaylistOrigin = _playlist.PlaylistId.Origin });
            
        }
    }
}
