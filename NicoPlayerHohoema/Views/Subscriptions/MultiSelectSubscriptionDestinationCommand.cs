using Microsoft.Toolkit.Uwp.UI;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Views.Subscriptions
{
    public sealed class MultiSelectSubscriptionDestinationCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Models.Subscription.Subscription;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Models.Subscription.Subscription subscription)
            {
                var dialogService = Commands.HohoemaCommnadHelper.GetDialogService();
                var hohoemaApp = Commands.HohoemaCommnadHelper.GetHohoemaApp();
                var playlistMan = hohoemaApp.Playlist;
                var mylistMan = hohoemaApp.UserMylistManager;

                // 既に登録済みのアイテムを先頭にして
                // 続いてマイリスト、ローカルプレイリストと表示する
                var playlists = new List<Models.IPlayableList>()
                {
                    playlistMan.DefaultPlaylist
                };

                foreach (var playlist in mylistMan.UserMylists)
                {
                    playlists.Add(playlist);
                }

                foreach (var playlist in playlistMan.Playlists)
                {
                    playlists.Add(playlist);
                }

                var selectedItems = new List<Models.IPlayableList>();
                foreach (var dest in subscription.Destinations.Reverse())
                {
                    var list = hohoemaApp.GetPlayableListInLocal(dest.PlaylistId, dest.Target == Models.Subscription.SubscriptionDestinationTarget.LocalPlaylist ? PlaylistOrigin.Local : PlaylistOrigin.LoginUser);

                    if (list != null)
                    {
                        playlists.Remove(list);
                        playlists.Insert(0, list);

                        selectedItems.Add(list);
                    }
                }


                var choiceItems = await dialogService.ShowMultiChoiceDialogAsync(
                    $"購読『{subscription.Label}』の新着追加先を選択",
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
                        choiceItem.Origin == PlaylistOrigin.Local ? Models.Subscription.SubscriptionDestinationTarget.LocalPlaylist : Models.Subscription.SubscriptionDestinationTarget.LoginUserMylist,
                        choiceItem.Id
                        );
                    subscription.Destinations.Add(dest);
                }
            }
        }
    }
}
