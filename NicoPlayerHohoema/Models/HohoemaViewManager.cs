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
using Windows.Foundation.Metadata;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

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

                    Window.Current.Activate();
                    await ApplicationViewSwitcher.TryShowAsStandaloneAsync(
                        id,
                        ViewSizePreference.Default,
                        MainView.Id,
                        ViewSizePreference.Default
                        );

                    view.Consolidated += SecondaryAppView_Consolidated;
                });
                ViewId = id;
                AppView = view;
                _SecondaryViewVM = vm;
                CoreAppView = playerView;
                NavigationService = ns;


            }

            return this;
        }


        private void SecondaryAppView_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
             NavigationService.Navigate(nameof(Views.BlankPage), null);
        }

        public async Task OpenContent(PlaylistItem item, bool withActivationWindow)
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

                if (withActivationWindow)
                {
//                    Window.Current.Activate();
                    await ApplicationViewSwitcher.TryShowAsStandaloneAsync(
                        ViewId,
                        ViewSizePreference.Default,
                        MainView.Id,
                        ViewSizePreference.Default
                        );
                }
            });
        }

        public Task ShowMainView()
        {
            if (AppView != null)
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
