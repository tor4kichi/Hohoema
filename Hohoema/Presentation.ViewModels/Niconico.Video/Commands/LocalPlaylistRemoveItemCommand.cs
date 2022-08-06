﻿
using Hohoema.Models.Domain.LocalMylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class LocalPlaylistRemoveItemCommand : VideoContentSelectionCommandBase
    {
        private readonly LocalPlaylist _playlist;

        public LocalPlaylistRemoveItemCommand(LocalPlaylist playlist)
        {
            _playlist = playlist;
        }

        protected override void Execute(IVideoContent content)
        {
            if (content is IPlaylistItemPlayable playableItem)
            _playlist.RemovePlaylistItem(playableItem.PlaylistItemToken);
        }

        protected override void Execute(IEnumerable<IVideoContent> items)
        {
            _playlist.RemovePlaylistItems(items.Select(x => (x as IPlaylistItemPlayable).PlaylistItemToken));
        }
    }
}
