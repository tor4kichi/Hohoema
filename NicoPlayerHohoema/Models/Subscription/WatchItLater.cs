using NicoPlayerHohoema.Helpers;
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

namespace NicoPlayerHohoema.Models.Subscription
{
    public class WatchItLater : BindableBase, IDisposable
    {
        public static WatchItLater Instance { get; } = new WatchItLater(SubscriptionManager.Instance);




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



        SubscriptionManager _SubscriptionManager;
        CompositeDisposable _disposables = new CompositeDisposable();
        IDisposable _autoUpdateDisposer;




        private WatchItLater(SubscriptionManager subscriptionManager)
        {
            _SubscriptionManager = subscriptionManager;

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

        


        public void Dispose()
        {
            _disposables.Dispose();
            _autoUpdateDisposer?.Dispose();
        }


        AsyncLock _UpdateLock = new AsyncLock();

        private async void StartOrResetAutoUpdate()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                _autoUpdateDisposer?.Dispose();
                _autoUpdateDisposer = null;

                if (AutoUpdateInterval == TimeSpan.Zero)
                {
                    AutoUpdateInterval = DefaultAutoUpdateInterval;
                }

                Debug.WriteLine("購読自動更新をリセット: " + AutoUpdateInterval.ToString());

                _autoUpdateDisposer = Observable.Interval(AutoUpdateInterval)
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
                // TODO: 前回更新時間から15分以上経っているか確認
                return _SubscriptionManager.GetSubscriptionFeedResultAsObservable()
                    .Subscribe(info =>
                    {
                        if (info.IsFirstUpdate)
                        {
                            Debug.WriteLine($"購読 {info.Subscription.Label} - {info.Source.Label} の通知をスキップ（理由：初回更新）");
                            return;
                        }

                        if (info.NewFeedItems?.Any() ?? false)
                        {
                            // NGキーワードを含むタイトルの動画を取り除いて
                            var filterdNewItems = info.NewFeedItems.Where(x => !info.Subscription.IsContainDoNotNoticeKeyword(x.Title));

                            Debug.WriteLine($"{info.Subscription.Label} - {info.Source.Label} -> {string.Join(",", filterdNewItems.Select(x => x.RawVideoId))}");

                            // 新着動画を対象プレイリストに追加
                            NewVideosAddToDestinations(info.Subscription.Destinations, filterdNewItems);
                        }
                    });
            }
        }


        private static void NewVideosAddToDestinations(IEnumerable<SubscriptionDestination> destinations, IEnumerable<Database.NicoVideo> newItems)
        {
            var hohoemaPlaylist = (App.Current as App).Container.Resolve<HohoemaPlaylist>();
            var mylistMan = (App.Current as App).Container.Resolve<UserMylistManager>();

            var hohoemaApp = (App.Current as App).Container.Resolve<HohoemaApp>();
            
            // あとで見るに追加
            foreach (var dest in destinations)
            {
                if (dest.Target == SubscriptionDestinationTarget.LocalPlaylist)
                {
                    var localMylist = hohoemaApp.GetPlayableListInLocal(dest.PlaylistId, PlaylistOrigin.Local) as LocalMylist;
                    if (localMylist == null)
                    {
                        return;
                    }
                    foreach (var video in newItems)
                    {
                        localMylist.AddVideo(video.RawVideoId, video.Title);
                    }
                }
                else if (dest.Target == SubscriptionDestinationTarget.LoginUserMylist)
                {
                    var localMylist = hohoemaApp.GetPlayableListInLocal(dest.PlaylistId, PlaylistOrigin.LoginUser) as MylistGroupInfo;
                    var userMylist = mylistMan.GetMylistGroup(dest.PlaylistId);
                    if (userMylist == null) { throw new Exception(); }

                    foreach (var video in newItems)
                    {
                        userMylist.Registration(video.RawVideoId);
                    }
                }
            }
        }

    }
}
