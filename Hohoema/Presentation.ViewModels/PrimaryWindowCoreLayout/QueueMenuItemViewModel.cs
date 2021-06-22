
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.Playlist;
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
    public sealed class QueueMenuItemViewModel : HohoemaListingPageItemBase, IPageNavigatable
    {
        private readonly HohoemaPlaylist _hohoemaPlaylist;

        public QueueMenuItemViewModel(HohoemaPlaylist hohoemaPlaylist)
        {
            _hohoemaPlaylist = hohoemaPlaylist;
            QueuePlaylistCount = _hohoemaPlaylist.QueuePlaylist
                .ObserveProperty(x => x.Count)
                .Do(_ => PlayQueuePlaylistCommand.RaiseCanExecuteChanged())
                .ToReactiveProperty()
                ;
        }

        public ReactiveProperty<int> QueuePlaylistCount { get; }


        private DelegateCommand _PlayQueuePlaylistCommand;
        public DelegateCommand PlayQueuePlaylistCommand =>
            _PlayQueuePlaylistCommand ?? (_PlayQueuePlaylistCommand = new DelegateCommand(ExecutePlayQueuePlaylistCommand, CanExecutePlayQueuePlaylistCommand));

        public HohoemaPageType PageType => HohoemaPageType.VideoQueue;

        public INavigationParameters Parameter => null;

        void ExecutePlayQueuePlaylistCommand()
        {
            _hohoemaPlaylist.Play(_hohoemaPlaylist.QueuePlaylist);
        }

        bool CanExecutePlayQueuePlaylistCommand()
        {
            return _hohoemaPlaylist.QueuePlaylist.Count > 0;
        }
    }
}
