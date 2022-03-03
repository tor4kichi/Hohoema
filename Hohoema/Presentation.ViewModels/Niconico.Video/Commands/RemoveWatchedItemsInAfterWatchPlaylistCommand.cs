using Hohoema.Models.Domain.Niconico.Video.WatchHistory.LoginUser;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase.Playlist;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class RemoveWatchedItemsInAfterWatchPlaylistCommand : CommandBase
    {
        private readonly QueuePlaylist _queuePlaylist;
        private readonly VideoPlayedHistoryRepository _videoPlayedHistoryRepository;

        public RemoveWatchedItemsInAfterWatchPlaylistCommand(
            QueuePlaylist queuePlaylist,
            VideoPlayedHistoryRepository videoPlayedHistoryRepository
            )
        {
            _queuePlaylist = queuePlaylist;
            _videoPlayedHistoryRepository = videoPlayedHistoryRepository;
        }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            int count = 0;
            foreach (var item in _queuePlaylist.ToArray())
            {
                if (_videoPlayedHistoryRepository.IsVideoPlayed(item.VideoId))
                {
                    _queuePlaylist.Remove(item);
                    count++;
                }
            }

            System.Diagnostics.Debug.WriteLine($"あとで見るから視聴済みを削除 （件数：{count}）");
        }
    }
}
