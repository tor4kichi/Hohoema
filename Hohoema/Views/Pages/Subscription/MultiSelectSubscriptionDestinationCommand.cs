using I18NPortable;
using Microsoft.Toolkit.Uwp.UI;
using Hohoema.Interfaces;
using Hohoema.Models;
using Hohoema.Models.Helpers;
using Hohoema.Models.LocalMylist;
using Hohoema.Services;
using Hohoema.Services.Helpers;
using Hohoema.UseCase.Playlist;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Views.Subscriptions
{
    public sealed class MultiSelectSubscriptionDestinationCommand : DelegateCommandBase
    {
        private readonly PlaylistAggregateGetter _playlistAggregateGetter;

        public MultiSelectSubscriptionDestinationCommand(
            DialogService dialogService,
            LocalMylistManager localMylistManager,
            UserMylistManager userMylistManager,
            HohoemaPlaylist hohoemaPlaylist,
            PlaylistAggregateGetter playlistAggregateGetter
            )
        {
            DialogService = dialogService;
            LocalMylistManager = localMylistManager;
            UserMylistManager = userMylistManager;
            HohoemaPlaylist = hohoemaPlaylist;
            _playlistAggregateGetter = playlistAggregateGetter;
        }

        public DialogService DialogService { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public UserMylistManager UserMylistManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }

        protected override bool CanExecute(object parameter)
        {
            return parameter is Models.Subscription.Subscription;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Models.Subscription.Subscription subscription)
            {
                // 既に登録済みのアイテムを先頭にして
                // 続いてマイリスト、ローカルプレイリストと表示する
                var playlists = new List<Interfaces.IPlaylist>()
                {
                    HohoemaPlaylist.WatchAfterPlaylist
                };

                foreach (var playlist in UserMylistManager.Mylists)
                {
                    playlists.Add(playlist);
                }

                foreach (var playlist in LocalMylistManager.LocalPlaylists)
                {
                    playlists.Add(playlist);
                }

                var selectedItems = new List<Interfaces.IPlaylist>();
                foreach (var dest in subscription.Destinations.Reverse())
                {
                    var list = await _playlistAggregateGetter.FindPlaylistAsync(dest.PlaylistId);

                    if (list != null)
                    {
                        playlists.Remove(list);
                        playlists.Insert(0, list);

                        selectedItems.Add(list);
                    }
                }


                var choiceItems = await DialogService.ShowMultiChoiceDialogAsync(
                    $"SelectSubscriptionGroup".Translate(),
                    playlists,
                    selectedItems,
                    x => x.Label
                    );

                if (choiceItems == null) { return; }

                subscription.Destinations.Clear();
                foreach (var choiceItem in choiceItems)
                {
                    var dest = new Models.Subscription.SubscriptionDestination(
                        choiceItem.Label,
                        choiceItem.GetOrigin() == PlaylistOrigin.Local ? Models.Subscription.SubscriptionDestinationTarget.LocalPlaylist : Models.Subscription.SubscriptionDestinationTarget.LoginUserMylist,
                        choiceItem.Id
                        );
                    subscription.Destinations.Add(dest);
                }
            }
        }
    }
}
