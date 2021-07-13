
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
using Microsoft.Toolkit.Mvvm.Messaging;
using Hohoema.Models.Domain.Playlist;

namespace Hohoema.Presentation.ViewModels.PrimaryWindowCoreLayout
{
    public sealed class QueueMenuItemViewModel : HohoemaListingPageItemBase, IPageNavigatable
    {
        public QueueMenuItemViewModel(QueuePlaylist queuePlaylist, IMessenger messenger)
        {
            _queuePlaylist = queuePlaylist;
            _messenger = messenger;
            QueuePlaylistCount = _queuePlaylist
                .ObserveProperty(x => x.Count)
                .Do(_ => PlayQueuePlaylistCommand.RaiseCanExecuteChanged())
                .ToReactiveProperty()
                ;
        }

        public ReactiveProperty<int> QueuePlaylistCount { get; }


        private DelegateCommand _PlayQueuePlaylistCommand;
        private readonly QueuePlaylist _queuePlaylist;
        private readonly IMessenger _messenger;

        public DelegateCommand PlayQueuePlaylistCommand =>
            _PlayQueuePlaylistCommand ?? (_PlayQueuePlaylistCommand = new DelegateCommand(ExecutePlayQueuePlaylistCommand, CanExecutePlayQueuePlaylistCommand));

        public HohoemaPageType PageType => HohoemaPageType.VideoQueue;

        public INavigationParameters Parameter => null;

        void ExecutePlayQueuePlaylistCommand()
        {
            if (_queuePlaylist.Any())
            {
                _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(_queuePlaylist));
            }
        }

        bool CanExecutePlayQueuePlaylistCommand()
        {
            return true;
        }
    }
}
