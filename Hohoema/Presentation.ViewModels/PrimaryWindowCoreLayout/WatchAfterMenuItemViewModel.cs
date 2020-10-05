
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.NicoVideos;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Domain.PageNavigation;

namespace Hohoema.Presentation.ViewModels.PrimaryWindowCoreLayout
{
    public sealed class WatchAfterMenuItemViewModel : HohoemaListingPageItemBase, IPageNavigatable
    {
        private readonly HohoemaPlaylist _hohoemaPlaylist;

        public WatchAfterMenuItemViewModel(HohoemaPlaylist hohoemaPlaylist)
        {
            _hohoemaPlaylist = hohoemaPlaylist;
            WatchAfterPlaylistCount = _hohoemaPlaylist.WatchAfterPlaylist
                .ObserveProperty(x => x.Count)
                .Do(_ => PlayWatchAfterPlaylistCommand.RaiseCanExecuteChanged())
                .ToReactiveProperty()
                ;
        }

        public ReactiveProperty<int> WatchAfterPlaylistCount { get; }


        private DelegateCommand _PlayWatchAfterPlaylistCommand;
        public DelegateCommand PlayWatchAfterPlaylistCommand =>
            _PlayWatchAfterPlaylistCommand ?? (_PlayWatchAfterPlaylistCommand = new DelegateCommand(ExecutePlayWatchAfterPlaylistCommand, CanExecutePlayWatchAfterPlaylistCommand));

        public HohoemaPageType PageType => HohoemaPageType.WatchAfter;

        public INavigationParameters Parameter => null;

        void ExecutePlayWatchAfterPlaylistCommand()
        {
            _hohoemaPlaylist.Play(_hohoemaPlaylist.WatchAfterPlaylist);
        }

        bool CanExecutePlayWatchAfterPlaylistCommand()
        {
            return _hohoemaPlaylist.WatchAfterPlaylist.Count > 0;
        }
    }
}
