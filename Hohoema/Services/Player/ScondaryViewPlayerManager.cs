﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Unity;
using System.Reactive.Concurrency;
using Prism.Mvvm;
using Microsoft.Toolkit.Uwp.Helpers;
using Prism.Events;
using Prism.Commands;
using System.Reactive;
using System.Threading;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Prism.Navigation;
using Prism.Unity;
using Windows.UI.Xaml.Media.Animation;
using Prism.Services;
using Hohoema.UseCase.Playlist;
using Hohoema.Interfaces;
using Hohoema.Models.Repository;
using Hohoema.Models.Helpers;

namespace Hohoema.Services.Player
{
    public sealed class ScondaryViewPlayerManager : FixPrism.BindableBase
    {
        /* 複数ウィンドウでプレイヤーを一つだけ表示するための管理をしています
         * 
         * 前提としてUnityContainer（PrismUnityApplication App.xaml.cs を参照）による依存性解決を利用しており
         * さらに「PerThreadLifetimeManager」を指定して依存解決時にウィンドウのUIスレッドごとに
         * PlayerViewManagerが生成されるように設定しています。
         * 
         * これはBindableBaseによるINofityPropertyChangedイベントがウィンドウスレッドを越えて利用できないことが理由です。
         * 
         * PlayerViewManagerはNowPlayingとPlayerViewModeの２つを公開プロパティとして保持しています。
         * NowPlaying
         * 
        */

        // プレイヤーの表示状態を管理する
        // これまでHohoemaPlaylist、MenuNavigationViewModelBaseなどに散らばっていた

        public ScondaryViewPlayerManager(
            IScheduler scheduler,
            IEventAggregator eventAggregator
            )
        {
            _scheduler = scheduler;
            EventAggregator = eventAggregator;
            MainViewId = ApplicationView.GetApplicationViewIdForWindow(CoreApplication.MainView.CoreWindow);
        }

        Models.Helpers.AsyncLock _playerNavigationLock = new Models.Helpers.AsyncLock();

        public int MainViewId { get; }

        public CoreApplicationView SecondaryCoreAppView { get; private set; }
        public ApplicationView SecondaryAppView { get; private set; }
        public INavigationService SecondaryViewPlayerNavigationService { get; private set; }

        public IEventAggregator EventAggregator { get; }

        //private PlayerViewMode? _PlayerViewMode;
        //public PlayerViewMode PlayerViewMode
        //{
        //    get
        //    {
        //        if (_PlayerViewMode != null) { return _PlayerViewMode.Value; }

        //        var localObjectStorageHelper = new LocalObjectStorageHelper();
        //        _PlayerViewMode = localObjectStorageHelper.Read(nameof(PlayerViewMode), PlayerViewMode.PrimaryView);
                
        //        return _PlayerViewMode.Value;
        //    }
        //    private set
        //    {
        //        CurrentViewScheduler.Schedule(() =>
        //        {
        //            if (SetProperty(ref _PlayerViewMode, value))
        //            {
        //                var localObjectStorageHelper = new LocalObjectStorageHelper();
        //                localObjectStorageHelper.Save(nameof(PlayerViewMode), _PlayerViewMode.Value);

        //                RaisePropertyChanged(nameof(IsPlayerShowWithPrimaryView));
        //                RaisePropertyChanged(nameof(IsPlayerShowWithSecondaryView));
        //                RaisePropertyChanged(nameof(IsPlayingWithPrimaryView));
        //                RaisePropertyChanged(nameof(IsPlayingWithSecondaryView));

        //                // PlayerViewMode変更後の動画再生を再開
        //                EventAggregator.GetEvent<PlayerViewModeChangeEvent>()
        //                    .Publish(new PlayerViewModeChangeEventArgs()
        //                    {
        //                        ViewMode = _PlayerViewMode.Value
        //                    });
        //            }
        //        });
        //    }
        //}

        //public bool IsPlayerShowWithPrimaryView => PlayerViewMode == PlayerViewMode.PrimaryView;
        //public bool IsPlayerShowWithSecondaryView => PlayerViewMode == PlayerViewMode.SecondaryView;

        //public bool IsPlayingWithPrimaryView => NowPlaying && PlayerViewMode == PlayerViewMode.PrimaryView;
        //public bool IsPlayingWithSecondaryView => NowPlaying && PlayerViewMode == PlayerViewMode.SecondaryView;

        //private bool _IsPlayerSmallWindowModeEnabled;
        //public bool IsPlayerSmallWindowModeEnabled
        //{
        //    get { return _IsPlayerSmallWindowModeEnabled; }
        //    set
        //    {
        //        CurrentViewScheduler.Schedule(() => 
        //        {
        //            if (SetProperty(ref _IsPlayerSmallWindowModeEnabled, value))
        //            {
        //                ToggleFullScreenWhenApplicationViewShowWithStandalone();
        //            }
        //        });
        //    }
        //}
        
        // メインビューを閉じたらプレイヤービューも閉じる
        private async void MainView_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            if (sender.Id == MainViewId)
            {
                if (SecondaryCoreAppView != null)
                {
                    await SecondaryCoreAppView.ExecuteOnUIThreadAsync(async () =>
                    {
                        if (SecondaryAppView != null)
                        {
                            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
                            {
                                await SecondaryAppView.TryConsolidateAsync();
                            }
                            else
                            {
                                SecondaryAppView.TryConsolidateAsync();
                            }
                        }
                    });
                }
            }
        }

        public bool IsShowSecondaryView { get; private set; }

        public const string primary_view_size = "primary_view_size";
        public const string secondary_view_size = "secondary_view_size";
        private readonly IScheduler _scheduler;

        private async Task CreateSecondaryView()
        {
            var secondaryView = CoreApplication.CreateNewView();
                            
            var result = await secondaryView.ExecuteOnUIThreadAsync(async () =>
            {
                secondaryView.TitleBar.ExtendViewIntoTitleBar = true;

                var content = new Views.SecondaryViewCoreLayout();

                var ns = content.CreateNavigationService();
                await ns.NavigateAsync(nameof(Views.BlankPage));

                Window.Current.Content = content;

                var id = ApplicationView.GetApplicationViewIdForWindow(secondaryView.CoreWindow);

                var view = ApplicationView.GetForCurrentView();

                view.TitleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                view.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

                await Task.Delay(250);

                Window.Current.Activate();

                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(id, ViewSizePreference.UseHalf, MainViewId, ViewSizePreference.UseHalf);
                
                // ウィンドウサイズの保存と復元
                if (DeviceTypeHelper.IsDesktop)
                {
                    var localObjectStorageHelper = App.Current.Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                    if (localObjectStorageHelper.KeyExists(secondary_view_size))
                    {
                        view.TryResizeView(localObjectStorageHelper.Read<Size>(secondary_view_size));
                    }

                    view.VisibleBoundsChanged += View_VisibleBoundsChanged;
                }

                view.Consolidated += SecondaryAppView_Consolidated;

                return (id, view, ns);
            });

            SecondaryAppView = result.view;
            SecondaryCoreAppView = secondaryView;
            SecondaryViewPlayerNavigationService = result.ns;

            var primaryView = ApplicationView.GetForCurrentView();
            primaryView.Consolidated += MainView_Consolidated;
        }

        // アプリ終了時に正しいウィンドウサイズを保存するための一時的な箱
        private Size _PrevSecondaryViewSize;

        /// <summary>
        /// ウィンドウサイズが変更された<br />
        /// 前回表示のウィンドウサイズの保存操作を実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void View_VisibleBoundsChanged(ApplicationView sender, object args)
        {
            // ウィンドウサイズを保存する
            if (sender.Id != MainViewId)
            {
                var localObjectStorageHelper = App.Current.Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                _PrevSecondaryViewSize = localObjectStorageHelper.Read<Size>(secondary_view_size);
                localObjectStorageHelper.Save(secondary_view_size, new Size(sender.VisibleBounds.Width, sender.VisibleBounds.Height));
            }

            Debug.WriteLine($"SecondaryView VisibleBoundsChanged: {sender.VisibleBounds.ToString()}");
        }

        private async void SecondaryAppView_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
             await SecondaryViewPlayerNavigationService.NavigateAsync(nameof(Views.BlankPage), new SuppressNavigationTransitionInfo());

            // Note: 1803時点での話
            // VisibleBoundsChanged がアプリ終了前に呼ばれるが
            // この際メインウィンドウとセカンダリウィンドウのウィンドウサイズが互い違いに送られてくるため
            // 直前のウィンドウサイズの値を前々回表示のウィンドウサイズ（_PrevSecondaryViewSize）で上書きする
            if (_PrevSecondaryViewSize != default(Size))
            {
                var localObjectStorageHelper = App.Current.Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                localObjectStorageHelper.Save(secondary_view_size, _PrevSecondaryViewSize);
            }

            _scheduler.Schedule(() => 
            {
                IsShowSecondaryView = false;
            });
        }

        static INiconicoContent _CurrentPlayContent { get; set; }

        bool _onceSurpressActivation;
        public void OnceSurpressActivation()
        {
            _onceSurpressActivation = true;
        }

        public async Task NavigationAsync(string pageName, INavigationParameters parameters)
        {
            using (await _playerNavigationLock.LockAsync())
            {
                if (SecondaryCoreAppView == null)
                {
                    await CreateSecondaryView();
                }

                await SecondaryCoreAppView.ExecuteOnUIThreadAsync(async () =>
                {
                    var result = await SecondaryViewPlayerNavigationService.NavigateAsync(pageName, parameters, new DrillInNavigationTransitionInfo());
                    if (result.Success)
                    {
                        var name = ResolveContentName(pageName, parameters);
                        SecondaryAppView.Title = name != null ? $"{name}" : "Hohoema";
                    }
                    else
                    {
                        Debug.WriteLine(result.Exception?.ToString());
                        await CloseAsync();
                        throw result.Exception;
                    }

                    if (SecondaryAppView.ViewMode != ApplicationViewMode.CompactOverlay)
                    {
                        await ApplicationViewSwitcher.TryShowAsStandaloneAsync(this.SecondaryAppView.Id, ViewSizePreference.Default, MainViewId, ViewSizePreference.UseNone);
                    }
                });

//                await ShowSecondaryViewAsync();

                IsShowSecondaryView = true;
            }
        }


        string ResolveContentName(string pageName, INavigationParameters parameters)
        {
            if (pageName == nameof(Views.VideoPlayerPage))
            {
                if (parameters.TryGetValue("id", out string videoId))
                {
                    var videoData = Database.NicoVideoDb.Get(videoId);
                    return videoData.Title;
                }
            }

            return null;
        }

        /// <summary>
        /// SecondaryViewを閉じます。
        /// MainViewから呼び出すと別スレッドアクセスでエラーになるはず
        /// </summary>
        public async Task CloseAsync()
        {
            if (!IsShowSecondaryView) { return; }

            await SecondaryCoreAppView.ExecuteOnUIThreadAsync(async () =>
            {
                SecondaryAppView.Title = "Hohoema";

                await ShowMainViewAsync();

                await SecondaryViewPlayerNavigationService.NavigateAsync(nameof(Views.BlankPage), new SuppressNavigationTransitionInfo());

                await SecondaryAppView.TryConsolidateAsync();
            });
        }

        public Task ShowMainViewAsync()
        {
            var currentView = ApplicationView.GetForCurrentView();
            if (SecondaryAppView != null && currentView.Id == SecondaryAppView.Id)
            {
                return ApplicationViewSwitcher.TryShowAsStandaloneAsync(MainViewId).AsTask();
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        public Task ShowSecondaryViewAsync()
        {
            if (!IsShowSecondaryView) { return Task.CompletedTask; }
            if (_onceSurpressActivation)
            {
                _onceSurpressActivation = false;
                return ShowMainViewAsync();
            }

            if (SecondaryAppView.ViewMode == ApplicationViewMode.CompactOverlay
                    || SecondaryAppView.IsFullScreenMode
                    )
            {
                return Task.CompletedTask;
            }

            var currentView = ApplicationView.GetForCurrentView();
            if (SecondaryAppView != null && currentView.Id != SecondaryAppView.Id)
            {
                return ApplicationViewSwitcher.TryShowAsStandaloneAsync(SecondaryAppView.Id).AsTask();
            }
            else
            {
                return Task.CompletedTask;
            }
        }


        public async Task ToggleCompactOverlayAsync()
        {
            if (!IsShowSecondaryView) { return; }

            if (!SecondaryAppView.IsViewModeSupported(ApplicationViewMode.CompactOverlay)) { return; }

            await SecondaryCoreAppView.ExecuteOnUIThreadAsync(async () =>
            {
                if (SecondaryAppView.ViewMode == ApplicationViewMode.Default)
                {
                    await SecondaryAppView.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
                }
                else
                {
                    await SecondaryAppView.TryEnterViewModeAsync(ApplicationViewMode.Default);
                }
            });
        }

        public async Task ToggleFullScreenAsync()
        {
            if (!IsShowSecondaryView) { return; }

            await SecondaryCoreAppView.ExecuteOnUIThreadAsync(() =>
            {
                if (SecondaryAppView.IsFullScreenMode)
                {
                    SecondaryAppView.ExitFullScreenMode();
                }
                else
                {
                    SecondaryAppView.TryEnterFullScreenMode();
                }
            });
        }
    }
}
