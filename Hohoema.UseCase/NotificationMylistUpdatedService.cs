﻿using Hohoema.Models;
using Hohoema.Models.Niconico;
using Hohoema.UseCase.Playlist;
using Hohoema.UseCase.Services;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services.Notification
{
    public sealed class NotificationMylistUpdatedService
    {
        public NotificationMylistUpdatedService(
            NiconicoSession niconicoSession,
            UserMylistManager userMylistManager,
            LocalMylistManager localMylistManager,
            IInAppNotificationService notificationService
            )
        {
            _niconicoSession = niconicoSession;
            _userMylistManager = userMylistManager;
            _localMylistManager = localMylistManager;
            notificationService = notificationService;

            Reset();
        }

        public bool IsNotifyMylistAdded { get; set; } = true;
        public bool IsNotifyMylistRemoved { get; set; } = true;
        public bool IsNotifyLocalMylistAdded { get; set; } = true;
        public bool IsNotifyLocalMylistRemoved { get; set; } = true;

        private readonly NiconicoSession _niconicoSession;
        private readonly UserMylistManager _userMylistManager;
        private readonly LocalMylistManager _localMylistManager;
        private readonly IInAppNotificationService notificationService;

        CompositeDisposable disposables = new CompositeDisposable();


        private void Reset()
        {
            // 【UserMylistManager】ログイン・ログアウトに反応して処理する
            //
            // 【LocalMylistManager】アプリ起動時に初期化されてる前提で処理する
            //


            //    // ローカルマイリストの
            //    foreach (var localMylist in LocalMylistManager.Mylists.ToArray())
            //    {
            //        AddHandleMylistItemsChanged(localMylist);
            //    }



            //    UserMylistManager.Mylists.CollectionChangedAsObservable()
            //        .Subscribe(e =>
            //        {
            //            switch (e.Action)
            //            {
            //                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
            //                    if (IsNotifyMylistAdded)
            //                    {
            //                        foreach (var item in e.NewItems.Cast<Interfaces.IUserOwnedMylist>())
            //                        {
            //                            AddHandleMylistItemsChanged(item);

            //                            // ログイン直後のマイリスト同期には反応しないように
            //                            if (!UserMylistManager.IsLoginUserMylistReady) { return; }

            //                            var text = $"マイリスト追加\n{item.Label}";
            //                            NotificationService.ShowInAppNotification(new InAppNotificationPayload()
            //                            {
            //                                Content = text,
            //                            });
            //                        }
            //                    }
            //                    break;
            //                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
            //                    break;
            //                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
            //                    if (IsNotifyMylistRemoved)
            //                    {
            //                        foreach (var item in e.OldItems.Cast<Interfaces.IUserOwnedMylist>())
            //                        {
            //                            RemoveHandleMylistItemsChanged(item);

            //                            // ログアウト直後のマイリストのクリアには反応しないように
            //                            if (!UserMylistManager.IsLoginUserMylistReady) { return; }

            //                            var text = $"マイリスト削除\n{item.Label}";
            //                            NotificationService.ShowInAppNotification(new InAppNotificationPayload()
            //                            {
            //                                Content = text,
            //                            });
            //                        }
            //                    }
            //                    break;
            //                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
            //                    break;
            //                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
            //                    // ログアウト時にインメモリのマイリスト情報を消した場合
            //                    {
            //                        // ログインユーザのマイリストに対する変更検出オブザーバを破棄
            //                        var remoteUserMylists = mylistItemsSubscriberMap
            //                        .Where(x => x.Key is Interfaces.IUserOwnedRemoteMylist)
            //                        .ToArray()
            //                        ;
            //                        foreach (var item in remoteUserMylists)
            //                        {
            //                            item.Value.Dispose();
            //                            mylistItemsSubscriberMap.Remove(item.Key);
            //                        }
            //                    }
            //                    break;
            //                default:
            //                    break;
            //            }
            //        })
            //        .AddTo(disposables);


            //    LocalMylistManager.Mylists.CollectionChangedAsObservable()
            //        .Subscribe(e =>
            //        {
            //            switch (e.Action)
            //            {
            //                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
            //                    if (IsNotifyLocalMylistAdded)
            //                    {
            //                        var item = e.NewItems.Cast<Interfaces.IUserOwnedMylist>().First();

            //                        AddHandleMylistItemsChanged(item);

            //                        var text = $"ローカルマイリスト追加\n{item.Label}";
            //                        NotificationService.ShowInAppNotification(new InAppNotificationPayload()
            //                        {
            //                            Content = text,
            //                        });
            //                    }
            //                    break;
            //                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
            //                    break;
            //                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
            //                    if (IsNotifyLocalMylistRemoved)
            //                    {
            //                        var item = e.OldItems.Cast<Interfaces.IUserOwnedMylist>().First();

            //                        RemoveHandleMylistItemsChanged(item);

            //                        var text = $"ローカルマイリスト削除\n{item.Label}";
            //                        NotificationService.ShowInAppNotification(new InAppNotificationPayload()
            //                        {
            //                            Content = text,
            //                        });
            //                    }
            //                    break;
            //                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
            //                    break;
            //                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
            //                    break;
            //                default:
            //                    break;
            //            }
            //        })
            //        .AddTo(disposables);
            //}


            //void AddHandleMylistItemsChanged(Interfaces.IUserOwnedMylist ownedMylist)
            //{
            //    if (mylistItemsSubscriberMap.ContainsKey(ownedMylist)) { return; }

            //    var disposable = ownedMylist.CollectionChangedAsObservable()
            //        .Subscribe(e =>
            //        {
            //            // ログイン直後の同期に反応しないように
            //            if (ownedMylist is UserOwnedMylist && !UserMylistManager.IsLoginUserMylistReady) { return; }

            //            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            //            {
            //                var item = e.NewItems.Cast<string>().First();

            //                var video = Database.NicoVideoDb.Get(item);
            //                if (video != null)
            //                {
            //                    var text = $"「{ownedMylist.Label}」に\n「{video.Title ?? video.RawVideoId}」を追加しました";
            //                    NotificationService.ShowInAppNotification(new InAppNotificationPayload()
            //                    {
            //                        Content = text,
            //                    });
            //                }
            //            }
            //            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            //            {
            //                var item = e.OldItems.Cast<string>().First();

            //                var video = Database.NicoVideoDb.Get(item);
            //                if (video != null)
            //                {
            //                    var text = $"「{ownedMylist.Label}」から\n「{video.Title ?? video.RawVideoId}」を削除しました";
            //                    NotificationService.ShowInAppNotification(new InAppNotificationPayload()
            //                    {
            //                        Content = text,
            //                    });
            //                }
            //            }
            //        });

            //    mylistItemsSubscriberMap.Add(ownedMylist, disposable);
            //}


            //void RemoveHandleMylistItemsChanged(Interfaces.IUserOwnedMylist ownedMylist)
            //{
            //    if (mylistItemsSubscriberMap.TryGetValue(ownedMylist, out var disposer))
            //    {
            //        disposer.Dispose();

            //        mylistItemsSubscriberMap.Remove(ownedMylist);
            //    }
            //}
        }

    }
}
