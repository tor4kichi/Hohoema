
using Hohoema.Models.Domain.Niconico;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist.Commands
{
    public sealed class WatchHistoryRemoveItemCommand : DelegateCommandBase
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
            return parameter is IWatchHistory;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is IWatchHistory watchHistory)
            {
                _ = _watchHistoryManager.RemoveHistoryAsync(watchHistory);
            }
        }
    }
}
