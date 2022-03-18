﻿
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.Playlist;
using Microsoft.Toolkit.Mvvm.Input;
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
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.Navigations;

namespace Hohoema.Presentation.ViewModels.PrimaryWindowCoreLayout
{
    public sealed class QueueMenuItemViewModel : HohoemaListingPageItemBase, IPageNavigatable
    {
        public QueueMenuItemViewModel(QueuePlaylist queuePlaylist, IMessenger messenger)
        {
            _queuePlaylist = queuePlaylist;
            _messenger = messenger;
            QueuePlaylistCount = _queuePlaylist
                .ObserveProperty(x => x.TotalCount)
                .Do(_ => PlayQueuePlaylistCommand.NotifyCanExecuteChanged())
                .ToReactiveProperty()
                ;
        }

        public ReactiveProperty<int> QueuePlaylistCount { get; }


        private RelayCommand _PlayQueuePlaylistCommand;
        private readonly QueuePlaylist _queuePlaylist;
        private readonly IMessenger _messenger;

        public RelayCommand PlayQueuePlaylistCommand =>
            _PlayQueuePlaylistCommand ?? (_PlayQueuePlaylistCommand = new RelayCommand(ExecutePlayQueuePlaylistCommand, CanExecutePlayQueuePlaylistCommand));

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
