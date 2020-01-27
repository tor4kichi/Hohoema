using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Playlist.Commands
{
    public sealed class WatchHistoryRemoveAllCommand : DelegateCommandBase
    {
        private readonly WatchHistoryManager _watchHistoryManager;

        public WatchHistoryRemoveAllCommand(
            WatchHistoryManager watchHistoryManager
            )
        {
            _watchHistoryManager = watchHistoryManager;
        }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            _ = _watchHistoryManager.RemoveAllHistoriesAsync();
        }
    }
}
