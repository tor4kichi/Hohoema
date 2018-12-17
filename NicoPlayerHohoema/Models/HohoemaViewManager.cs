using NicoPlayerHohoema.ViewModels;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using System;
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
using Microsoft.Practices.Unity;
using NicoPlayerHohoema.Models.Cache;

namespace NicoPlayerHohoema.Models
{
    public class HohoemaViewManager
    {
        public int ViewId { get; private set; }

        public ApplicationView MainView { get; private set; }

        public CoreApplicationView CoreAppView { get; private set; }
        public ApplicationView AppView { get; private set; }
        public INavigationService NavigationService { get; private set; }

        private HohoemaSecondaryViewFrameViewModel _SecondaryViewVM { get; set; }



        public bool NowShowingSecondaryView { get; private set; }

        private MediaPlayer _MediaPlayer { get; set; }
        private Window _CurrentMediaPlayerWindow;
        public MediaPlayer GetCurrentWindowMediaPlayer()
        {
            if (Window.Current != _CurrentMediaPlayerWindow)
            {
                if (_MediaPlayer != null)
                {
                    _MediaPlayer?.Dispose();
                }

                _MediaPlayer = new MediaPlayer();
                _MediaPlayer.AutoPlay = true;
                _CurrentMediaPlayerWindow = Window.Current;
            }

            return _MediaPlayer;
        }

        public HohoemaViewManager()
        {
            MainView = ApplicationView.GetForCurrentView();

            MainView.Consolidated += MainView_Consolidated;
        }

        private async void MainView_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            if (sender == MainView)
            {
                await CoreAppView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => 
                {
                    if (AppView != null)
                    {
                        if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
                        {
                            await AppView.TryConsolidateAsync();
                        }
                        else
                        {
                            App.Current.Exit();
                        }
                    }
                });
            }
        }

        public const string primary_view_size = "primary_view_size";
        public const string secondary_view_size = "secondary_view_size";


        private async Task<HohoemaViewManager> GetEnsureSecondaryView()
        {
            if (CoreAppView == null)
            {
                var playerView = CoreApplication.CreateNewView();

                
                HohoemaSecondaryViewFrameViewModel vm = null;
                int id = 0;
                ApplicationView view = null;
                INavigationService ns = null;
                await playerView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    playerView.TitleBar.ExtendViewIntoTitleBar = true;

                    var content = new Views.HohoemaSecondaryViewFrame();

                    var frameFacade = new FrameFacadeAdapter(content.Frame);

                    var sessionStateService = new SessionStateService();

                    ns = new FrameNavigationService(frameFacade
                        , (pageToken) =>
                        {
                            if (pageToken == nameof(Views.VideoPlayerPage))
                            {
                                return typeof(Views.VideoPlayerPage);
                            }
                            else if (pageToken == nameof(Views.LivePlayerPage))
                            {
                                return typeof(Views.LivePlayerPage);
                            }
                            else
                            {
                                return typeof(Views.BlankPage);
                            }
                        }, sessionStateService);


                    vm = content.DataContext as HohoemaSecondaryViewFrameViewModel;


                    Window.Current.Content = content;

                    id = ApplicationView.GetApplicationViewIdForWindow(playerView.CoreWindow);

                    view = ApplicationView.GetForCurrentView();

                    view.TitleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                    view.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

                    Window.Current.Activate();

                    await ApplicationViewSwitcher.TryShowAsStandaloneAsync(id, ViewSizePreference.Default, MainView.Id, ViewSizePreference.Default);

                    // ウィンドウサイズの保存と復元
                    if (Services.Helpers.DeviceTypeHelper.IsDesktop)
                    {
                        var localObjectStorageHelper = App.Current.Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                        if (localObjectStorageHelper.KeyExists(secondary_view_size))
                        {
                            view.TryResizeView(localObjectStorageHelper.Read<Size>(secondary_view_size));
                        }

                        view.VisibleBoundsChanged += View_VisibleBoundsChanged;
                    }

                    view.Consolidated += SecondaryAppView_Consolidated;

                });
                ViewId = id;
                AppView = view;
                _SecondaryViewVM = vm;
                CoreAppView = playerView;
                NavigationService = ns;
            }

            NowShowingSecondaryView = true;

            return this;
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
            if (sender.Id == AppView?.Id)
            {
                var localObjectStorageHelper = App.Current.Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                _PrevSecondaryViewSize = localObjectStorageHelper.Read<Size>(secondary_view_size);
                localObjectStorageHelper.Save(secondary_view_size, new Size(sender.VisibleBounds.Width, sender.VisibleBounds.Height));
            }

            Debug.WriteLine($"SecondaryView VisibleBoundsChanged: {sender.VisibleBounds.ToString()}");
        }

        private async void SecondaryAppView_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
             NavigationService.Navigate(nameof(Views.BlankPage), null);

            // Note: 1803時点での話
            // VisibleBoundsChanged がアプリ終了前に呼ばれるが
            // この際メインウィンドウとセカンダリウィンドウのウィンドウサイズが互い違いに送られてくるため
            // 直前のウィンドウサイズの値を前々回表示のウィンドウサイズ（_PrevSecondaryViewSize）で上書きする
            if (_PrevSecondaryViewSize != default(Size))
            {
                var localObjectStorageHelper = App.Current.Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                localObjectStorageHelper.Save(secondary_view_size, _PrevSecondaryViewSize);
            }

            NowShowingSecondaryView = false;


            // セカンダリウィンドウを閉じるタイミングでキャッシュを再開する
            // プレミアム会員の場合は何もおきない
            var cacheManager = App.Current.Container.Resolve<VideoCacheManager>();
            await cacheManager.ResumeCacheDownload();
        }

        public async Task OpenContent(PlaylistItem item)
        {
            var payload = new SecondaryViewNavigatePayload()
            {
                ContentId = item.ContentId,
                ContentType = item.Type.ToString(),
                Title = item.Title
            };

            var pageType = item.Type == PlaylistItemType.Video ? nameof(Views.VideoPlayerPage) : nameof(Views.LivePlayerPage);
            string parameter = null;
            if (item.Type == PlaylistItemType.Video)
            {
                parameter = new VideoPlayPayload()
                {
                    VideoId = item.ContentId
                }.ToParameterString();
            }
            else
            {
                parameter = new Models.Live.LiveVideoPagePayload(item.ContentId)
                {
                    LiveTitle = item.Title
                }
                    .ToParameterString();
            }



            await NavigatingSecondaryViewAsync(pageType, parameter);

            await CoreAppView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                AppView.Title = !string.IsNullOrEmpty(item?.Title) ? item.Title : "Hohoema";

                
                // Note: Can not complete automatically enter FullScreen on secondary window.
                // AppView.TryEnterFullScreenMode will return 'false'
                // how fix it ?

                /*
                await Task.Delay(1000);

                if (Services.Helpers.DeviceTypeHelper.IsMobile)
                {
                    AppView.TryEnterFullScreenMode();

                    // Note: Mobile is not support Secondary View.
                }
                else if (Services.Helpers.DeviceTypeHelper.IsDesktop)
                {
                    if (AppView.AdjacentToLeftDisplayEdge && AppView.AdjacentToRightDisplayEdge)
                    {
                        AppView.TryEnterFullScreenMode();
                    }
                }
                */

            });
        }

        public Task ShowMainView()
        {
            var currentView = ApplicationView.GetForCurrentView();
            if (AppView != null && currentView.Id != MainView.Id)
            {
                return ApplicationViewSwitcher.SwitchAsync(MainView.Id, ViewId).AsTask();
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        private async Task NavigatingSecondaryViewAsync(string pageType, object navigationParam = null)
        {
            // サブウィンドウをアクティベートして、サブウィンドウにPlayerページナビゲーションを飛ばす
            await GetEnsureSecondaryView();

            await CoreAppView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (NavigationService.Navigate(pageType, navigationParam))
                {
                    
                }
            });
        }

        /// <summary>
        /// SecondaryViewを閉じます。
        /// MainViewから呼び出すと別スレッドアクセスでエラーになるはず
        /// </summary>
        public async Task Close()
        {
            if (_SecondaryViewVM == null) { return; }

            NavigationService.Navigate(nameof(Views.BlankPage), null);

            await ShowMainView();

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                await AppView.TryConsolidateAsync().AsTask()
                    .ContinueWith(prevTask => 
                    {
//                        CoreAppView = null;
//                        AppView = null;
//                        _SecondaryViewVM = null;
//                        NavigationService = null;
                    });
            }
            else
            {
                AppView.Consolidated -= SecondaryAppView_Consolidated;

                CoreAppView.CoreWindow.Close();

                CoreAppView = null;
                AppView = null;
                _SecondaryViewVM = null;
                NavigationService = null;
            }

            
        }


    }
}
