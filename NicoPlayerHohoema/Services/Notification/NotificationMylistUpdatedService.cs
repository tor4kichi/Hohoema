using NicoPlayerHohoema.Models;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Services.Notification
{
    public sealed class NotificationMylistUpdatedService
    {
        public NotificationMylistUpdatedService(
            NiconicoSession niconicoSession,
            Models.UserMylistManager userMylistManager,
            Models.LocalMylist.LocalMylistManager localMylistManager,
            NotificationService notificationService
            )
        {
            NiconicoSession = niconicoSession;
            UserMylistManager = userMylistManager;
            LocalMylistManager = localMylistManager;
            NotificationService = notificationService;

            Reset();
        }

        public bool IsNotifyMylistAdded { get; set; } = true;
        public bool IsNotifyMylistRemoved { get; set; } = true;
        public bool IsNotifyLocalMylistAdded { get; set; } = true;
        public bool IsNotifyLocalMylistRemoved { get; set; } = true;

        public NiconicoSession NiconicoSession { get; }
        public Models.UserMylistManager UserMylistManager { get; }
        public Models.LocalMylist.LocalMylistManager LocalMylistManager { get; }
        public NotificationService NotificationService { get; }

        Dictionary<Interfaces.IUserOwnedMylist, IDisposable> mylistItemsSubscriberMap 
            = new Dictionary<Interfaces.IUserOwnedMylist, IDisposable>();

        CompositeDisposable disposables = new CompositeDisposable();


        private void Reset()
        {
            // 【UserMylistManager】ログイン・ログアウトに反応して処理する
            //
            // 【LocalMylistManager】アプリ起動時に初期化されてる前提で処理する
            //


            // 非同期操作で変更があってもいいように別配列に入れて処理する
            foreach (var localMylist in LocalMylistManager.Mylists.ToArray())
            {
                AddHandleMylistItemsChanged(localMylist);
            }



            UserMylistManager.Mylists.CollectionChangedAsObservable()
                .Subscribe(e =>
                {
                    switch (e.Action)
                    {
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                            if (IsNotifyMylistAdded)
                            {
                                foreach (var item in e.NewItems.Cast<Interfaces.IUserOwnedMylist>())
                                {
                                    AddHandleMylistItemsChanged(item);

                                    // ログイン直後のマイリスト同期には反応しないように
                                    if (!UserMylistManager.IsLoginUserMylistReady) { return; }

                                    var text = $"マイリスト追加\n{item.Label}";
                                    NotificationService.ShowInAppNotification(new InAppNotificationPayload()
                                    {
                                        Content = text,
                                    });
                                }
                            }
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                            if (IsNotifyMylistRemoved)
                            {
                                foreach (var item in e.OldItems.Cast<Interfaces.IUserOwnedMylist>())
                                {
                                    RemoveHandleMylistItemsChanged(item);

                                    // ログアウト直後のマイリストのクリアには反応しないように
                                    if (!UserMylistManager.IsLoginUserMylistReady) { return; }

                                    var text = $"マイリスト削除\n{item.Label}";
                                    NotificationService.ShowInAppNotification(new InAppNotificationPayload()
                                    {
                                        Content = text,
                                    });
                                }
                            }
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                            // ログアウト時にインメモリのマイリスト情報を消した場合
                            {
                                foreach (var item in mylistItemsSubscriberMap.Values.ToArray())
                                {
                                    item.Dispose();
                                }
                                mylistItemsSubscriberMap.Clear();
                            }
                            break;
                        default:
                            break;
                    }
                })
                .AddTo(disposables);


            LocalMylistManager.Mylists.CollectionChangedAsObservable()
                .Subscribe(e =>
                {
                    switch (e.Action)
                    {
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                            if (IsNotifyLocalMylistAdded)
                            {
                                var item = e.NewItems.Cast<Interfaces.IUserOwnedMylist>().First();

                                AddHandleMylistItemsChanged(item);

                                var text = $"ローカルマイリスト追加\n{item.Label}";
                                NotificationService.ShowInAppNotification(new InAppNotificationPayload()
                                {
                                    Content = text,
                                });
                            }
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                            if (IsNotifyLocalMylistRemoved)
                            {
                                var item = e.OldItems.Cast<Interfaces.IUserOwnedMylist>().First();

                                RemoveHandleMylistItemsChanged(item);

                                var text = $"ローカルマイリスト削除\n{item.Label}";
                                NotificationService.ShowInAppNotification(new InAppNotificationPayload()
                                {
                                    Content = text,
                                });
                            }
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                            break;
                        default:
                            break;
                    }
                })
                .AddTo(disposables);
        }


        void AddHandleMylistItemsChanged(Interfaces.IUserOwnedMylist ownedMylist)
        {
            if (mylistItemsSubscriberMap.ContainsKey(ownedMylist)) { return; }

            var disposable = ownedMylist.CollectionChangedAsObservable()
                .Subscribe(e =>
                {
                    // ログイン直後の同期に反応しないように
                    if (ownedMylist is UserOwnedMylist && !UserMylistManager.IsLoginUserMylistReady) { return; }

                    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                    {
                        var item = e.NewItems.Cast<string>().First();

                        var video = Database.NicoVideoDb.Get(item);
                        if (video != null)
                        {
                            var text = $"マイリスト「{ownedMylist.Label}」に\n「{video.Title ?? video.RawVideoId}」を追加しました";
                            NotificationService.ShowInAppNotification(new InAppNotificationPayload()
                            {
                                Content = text,
                            });
                        }
                    }
                    else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                    {
                        var item = e.OldItems.Cast<string>().First();

                        var video = Database.NicoVideoDb.Get(item);
                        if (video != null)
                        {
                            var text = $"マイリスト「{ownedMylist.Label}」から\n「{video.Title ?? video.RawVideoId}」を削除しました";
                            NotificationService.ShowInAppNotification(new InAppNotificationPayload()
                            {
                                Content = text,
                            });
                        }
                    }
                });

            mylistItemsSubscriberMap.Add(ownedMylist, disposable);
        }


        void RemoveHandleMylistItemsChanged(Interfaces.IUserOwnedMylist ownedMylist)
        {
            if (mylistItemsSubscriberMap.TryGetValue(ownedMylist, out var disposer))
            {
                disposer.Dispose();

                mylistItemsSubscriberMap.Remove(ownedMylist);
            }
        }


    }
}
