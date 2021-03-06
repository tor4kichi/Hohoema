﻿using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Player.Video
{
    public sealed class ChangeVideoQualityCommand : DelegateCommandBase
    {
        private readonly HohoemaPlaylistPlayer _playlistPlayer;

        public ChangeVideoQualityCommand(HohoemaPlaylistPlayer playlistPlayer)
        {
            _playlistPlayer = playlistPlayer;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is  NicoVideoQuality or NicoVideoQualityEntity;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is NicoVideoQuality quality)
            {
                if (!_playlistPlayer.CanPlayQuality(quality)) { return; }
                await _playlistPlayer.ChangeQualityAsync(quality);
            }
            else if (parameter is NicoVideoQualityEntity qualityEntity)
            {
                if (!_playlistPlayer.CanPlayQuality(qualityEntity.Quality)) { return; }
                await _playlistPlayer.ChangeQualityAsync(qualityEntity.Quality);
            }
        }
    }
}
