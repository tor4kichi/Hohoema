using NicoPlayerHohoema.Models.Helpers;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Reactive.Bindings;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using NicoPlayerHohoema.Models.Subscription;
using NicoPlayerHohoema.Models;
using System.Reactive.Concurrency;

namespace NicoPlayerHohoema.Services
{
    public class WatchItLater : BindableBase, IDisposable
    {
        public WatchItLater(
            IScheduler scheduler,
            SubscriptionManager subscriptionManager,
            Models.Provider.LoginUserHistoryProvider loginUserDataProvider,
            Services.Helpers.MylistHelper mylistHelper
            )
        {
            Scheduler = scheduler;
            SubscriptionManager = subscriptionManager;
            LoginUserDataProvider = loginUserDataProvider;
            MylistHelper = mylistHelper;
            Refresh = new AsyncReactiveCommand();
            Refresh.Subscribe(async () => await Update())
                .AddTo(_disposables);

            {
                var localObjectStorageHelper = new LocalObjectStorageHelper();
                IsAutoUpdateEnabled = localObjectStorageHelper.Read(nameof(IsAutoUpdateEnabled), true);
                AutoUpdateInterval = localObjectStorageHelper.Read(nameof(AutoUpdateInterval), DefaultAutoUpdateInterval);
            }

            this.ObserveProperty(x => x.IsAutoUpdateEnabled)
                .Subscribe(x =>
                {
                    var localObjectStorageHelper = new LocalObjectStorageHelper();
                    localObjectStorageHelper.Save(nameof(IsAutoUpdateEnabled), IsAutoUpdateEnabled);
                })
                .AddTo(_disposables);

            this.ObserveProperty(x => x.AutoUpdateInterval)
                .Subscribe(x =>
                {
                    var localObjectStorageHelper = new LocalObjectStorageHelper();
                    localObjectStorageHelper.Save(nameof(AutoUpdateInterval), AutoUpdateInterval);
                })
                .AddTo(_disposables);

            Observable.Merge(new[]
                {
                    this.ObserveProperty(x => x.IsAutoUpdateEnabled).ToUnit(),
                    this.ObserveProperty(x => x.AutoUpdateInterval).ToUnit()
                })
                .Subscribe(x =>
                {
                    if (IsAutoUpdateEnabled)
                    {
                        StartOrResetAutoUpdate();
                    }
                    else
                    {
                        StopAutoUpdate();
                    }
                })
                .AddTo(_disposables);
        }




        public static readonly TimeSpan DefaultAutoUpdateInterval = TimeSpan.FromMinutes(15);

        private bool _IsAutoUpdateEnabled = true;
        public bool IsAutoUpdateEnabled
        {
            get { return _IsAutoUpdateEnabled; }
            set { SetProperty(ref _IsAutoUpdateEnabled, value); }
        }


        private TimeSpan _AutoUpdateInterval = DefaultAutoUpdateInterval;
        public TimeSpan AutoUpdateInterval
        {
            get { return _AutoUpdateInterval; }
            set { SetProperty(ref _AutoUpdateInterval, value); }
        }



        public AsyncReactiveCommand Refresh { get; }
        public IScheduler Scheduler { get; }
        public SubscriptionManager SubscriptionManager { get; }
        public Models.Provider.LoginUserHistoryProvider LoginUserDataProvider { get; }
        public Services.Helpers.MylistHelper MylistHelper { get; }
        CompositeDisposable _disposables { get; } = new CompositeDisposable();

        IDisposable _autoUpdateDisposer;
        Models.Helpers.AsyncLock _UpdateLock = new Models.Helpers.AsyncLock();


        

        public void Dispose()
        {
            _disposables.Dispose();
            _autoUpdateDisposer?.Dispose();
        }


        public void Initialize()
        {
            StartOrResetAutoUpdate();
        }



        private async void StartOrResetAutoUpdate()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                _autoUpdateDisposer?.Dispose();
                _autoUpdateDisposer = null;

                if (!IsAutoUpdateEnabled) { return; }

                if (AutoUpdateInterval == TimeSpan.Zero)
                {
                    AutoUpdateInterval = DefaultAutoUpdateInterval;
                }

                Debug.WriteLine("購読自動更新をリセット: " + AutoUpdateInterval.ToString());

                _autoUpdateDisposer = Observable.Timer(TimeSpan.FromSeconds(5), AutoUpdateInterval)
                    .Subscribe(async _ =>
                    {
                        Debug.WriteLine("購読自動更新 処理開始");

                        await Update();

                        Debug.WriteLine("購読自動更新 処理完了");
                    });
            }
        }

        private async void StopAutoUpdate()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                _autoUpdateDisposer?.Dispose();
                _autoUpdateDisposer = null;
            }
        }



        private async Task<IDisposable> Update()
        {
            using (var relaser = await _UpdateLock.LockAsync())
            {
                NewItemsPerPlayableList.Clear();

                // 視聴履歴を取得して再生済み動画の情報を更新する
                _ = LoginUserDataProvider.GetHistory();

#if true
                // TODO: 前回更新時間から15分以上経っているか確認
                return SubscriptionManager.GetSubscriptionFeedResultAsObservable(withoutDisabled: true)
                    .Subscribe(info =>
                    {
                        Debug.WriteLine($"購読 {info.Subscription.Label} - {info.Source?.Label}");

                        if (!info.Subscription.IsEnabled)
                        {
                            Debug.WriteLine($"購読 {info.Subscription.Label} - {info.Source?.Label} の通知をスキップ（理由：自動更新が無効）");
                            return;
                        }

                        if (info.IsFirstUpdate)
                        {
                            Debug.WriteLine($"購読 {info.Subscription.Label} - {info.Source?.Label} の通知をスキップ（理由：初回更新）");
                            return;
                        }

                        if (info.NewFeedItems?.Any() ?? false)
                        {
                            // 新着動画を対象プレイリストに追加
                            Scheduler.Schedule(() => 
                            {
                                // NGキーワードを含むタイトルの動画を取り除いて
                                var filterdNewItems = info.NewFeedItems
                                    .Where(x => !WatchItLater.IsVideoPlayed(x.VideoId))
                                    .Where(x => !info.Subscription.IsContainDoNotNoticeKeyword(x.Title))
                                    .ToList()
                                    ;

                                Debug.WriteLine($"{info.Subscription.Label} - {info.Source?.Label} -> {string.Join(",", filterdNewItems.Select(x => x.RawVideoId))}");
                                NewVideosAddToDestinations(info.Subscription.Destinations, filterdNewItems);

                                // トースト通知を発行
                                ShowNewVideosToastNotification(info.Subscription, info.Source.Value, info.NewFeedItems);
                            });
                        }
                    });
#else
                // 通知テスト
                foreach (var subsc in SubscriptionManager.Instance.Subscriptions)
                {
                    var feedResult = Database.Local.Subscription.SubscriptionFeedResultDb.GetEnsureFeedResult(subsc);

                    var result = feedResult.GetFeedResultSet(subsc.Sources[0]);
                    var info = new SubscriptionUpdateInfo()
                    {
                        Subscription = subsc,
                        Source = subsc.Sources[0],
                        NewFeedItems = result.Items.Take(5).Select(x => Database.NicoVideoDb.Get(x)).ToList(),
                    };

                    ShowNewVideosToastNotification(info.Subscription, info.Source, info.NewFeedItems);
                }
                return new CompositeDisposable();
#endif

            }
        }

        // Note: ローカルなトースト通知の送信
        // https://docs.microsoft.com/ja-jp/windows/uwp/design/shell/tiles-and-notifications/send-local-toast#send-a-toast

        private const string TOAST_GROUP = nameof(WatchItLater);
        private const string TOAST_HEADER_ID = nameof(WatchItLater);

        private Dictionary<Interfaces.IMylist, Tuple<IList<Database.NicoVideo>, IList<Subscription>>> NewItemsPerPlayableList = new Dictionary<Interfaces.IMylist, Tuple<IList<Database.NicoVideo>, IList<Subscription>>>();
        

        private void ShowNewVideosToastNotification(Subscription subscription, SubscriptionSource source, IEnumerable<Database.NicoVideo> newItems)
        {
            var mylistMan = (App.Current as App).Container.Resolve<UserMylistManager>();

            // TODO: プレイリスト毎にまとめたほうがいい？

            foreach (var dest in subscription.Destinations)
            {
                Interfaces.IMylist list = null;
                if (dest.Target == SubscriptionDestinationTarget.LocalPlaylist)
                {
                    list = MylistHelper.FindMylistInCached(dest.PlaylistId, PlaylistOrigin.Local);
                }
                else if (dest.Target == SubscriptionDestinationTarget.LoginUserMylist)
                {
                    list = MylistHelper.FindMylistInCached(dest.PlaylistId, PlaylistOrigin.LoginUser);

                }

                IList<Database.NicoVideo> videoList;
                if (NewItemsPerPlayableList.TryGetValue(list, out var cacheVideoList))
                {
                    videoList = cacheVideoList.Item1;

                    if (!cacheVideoList.Item2.Contains(subscription))
                    {
                        cacheVideoList.Item2.Add(subscription);
                    }
                }
                else
                {
                    videoList = new List<Database.NicoVideo>();
                    NewItemsPerPlayableList.Add(list, new Tuple<IList<Database.NicoVideo>, IList<Subscription>>(videoList, new List<Subscription>() { subscription }));
                }

                foreach (var video in newItems)
                {
                    videoList.Add(video);
                }



            }


            try
            {
                foreach (var pair in NewItemsPerPlayableList)
                {
                    var playableList = pair.Key;
                    var newItemsPerList = pair.Value.Item1;
                    var subscriptions = pair.Value.Item2;

                    ToastVisual visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children = { }

                        }
                    };

                    visual.BindingGeneric.Children.Insert(0, new AdaptiveText()
                    {
                        Text = $"『{playableList.Label}』に新着動画を追加",
                        HintStyle = AdaptiveTextStyle.Base
                    });

                    foreach (var item in newItemsPerList)
                    {
                        visual.BindingGeneric.Children.Add(new AdaptiveText()
                        {
                            Text = item.Title,
                            HintStyle = AdaptiveTextStyle.BaseSubtle
                        });
                    }


                        
                    ToastActionsCustom action = new ToastActionsCustom()
                    {
                        Buttons =
                        {
                            new ToastButton("視聴する", new LoginRedirectPayload() { RedirectPageType = HohoemaPageType.VideoPlayer, RedirectParamter = newItemsPerList.First().RawVideoId }.ToParameterString())
                            {
                                ActivationType = ToastActivationType.Foreground,
                            },
                            new ToastButton("購読を管理", new LoginRedirectPayload() { RedirectPageType = HohoemaPageType.Subscription }.ToParameterString())
                            {
                                ActivationType = ToastActivationType.Foreground,
                            },
                        }
                    };

                    ToastContent toastContent = new ToastContent()
                    {
                        Visual = visual,
                        Actions = action,
                    };

                    var toast = new ToastNotification(toastContent.GetXml());

                    var notifier = ToastNotificationManager.CreateToastNotifier();

                    toast.Tag = playableList.Id;
                    toast.Group = TOAST_GROUP;
                    notifier.Show(toast);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }



        }

        


        private void NewVideosAddToDestinations(IEnumerable<SubscriptionDestination> destinations, IEnumerable<Database.NicoVideo> newItems)
        {            
            // あとで見るに追加
            foreach (var dest in destinations)
            {
                var origin = default(PlaylistOrigin?);
                if (dest.Target == SubscriptionDestinationTarget.LocalPlaylist)
                {
                    origin = PlaylistOrigin.Local;
                }
                else if (dest.Target == SubscriptionDestinationTarget.LoginUserMylist)
                {
                    origin = PlaylistOrigin.LoginUser;
                }

                var mylist = MylistHelper.FindMylistInCached(dest.PlaylistId, origin);
                if (mylist is Interfaces.IUserOwnedMylist ownedMylist)
                {
                    foreach (var video in newItems)
                    {
                        _ = ownedMylist.AddMylistItem(video.RawVideoId);
                    }
                }
            }
        }



        private static bool IsVideoPlayed(string videoId)
        {
            return Database.VideoPlayedHistoryDb.IsVideoPlayed(videoId);
        }
    }
}
