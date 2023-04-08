using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Video.WatchHistory.LoginUser;
using Hohoema.Services.Niconico;
using Hohoema.Services.Playlist;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Video.Commands
{
    public sealed class WatchHistoryRemoveItemCommand : CommandBase
    {
        private readonly WatchHistoryManager _watchHistoryManager;

        public WatchHistoryRemoveItemCommand(
            WatchHistoryManager watchHistoryManager
            )
        {
            _watchHistoryManager = watchHistoryManager;
        }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is IVideoContent watchHistory)
            {
                _ = _watchHistoryManager.RemoveHistoryAsync(watchHistory);
            }
            else if (parameter is IList histories)
            {
                await _watchHistoryManager.RemoveHistoryAsync(histories.Cast<IVideoContent>().ToList());
            }
        }
    }
}
