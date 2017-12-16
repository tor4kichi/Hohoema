using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Views.Controls;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Prism.Windows.Mvvm;
using Windows.UI.Xaml.Navigation;
using Prism.Commands;
using NicoPlayerHohoema.Models.Live;
using Windows.UI.ViewManagement;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Prism.Windows.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
    public class PlayerWithPageContainerViewModel : BindableBase
    {
        public HohoemaPlaylist HohoemaPlaylist { get; private set; }

        public ReadOnlyReactiveProperty<bool> IsFillFloatContent { get; private set; }
        public ReactiveProperty<bool> IsVisibleFloatContent { get; private set; }

        public ReactiveProperty<bool> IsContentDisplayFloating { get; private set; }

        private Dictionary<string, object> viewModelState = new Dictionary<string, object>();


        /// <summary>
        /// Playerの小窓状態の変化を遅延させて伝播させます、
        /// 
        /// 遅延させている理由は、Player上のFlyoutを閉じる前にリサイズが発生するとFlyoutが
        /// ゴースト化（FlyoutのUIは表示されず閉じれないが、Visible状態と同じようなインタラクションだけは出来てしまう）
        /// してしまうためです。（タブレット端末で発生、PCだと発生確認されず）
        /// この問題を回避するためにFlyoutが閉じられた後にプレイヤーリサイズが走るように遅延を挟んでいます。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsFillFloatContent_DelayedRead { get; private set; }


        public INavigationService NavigationService { get; private set; }

        public void SetNavigationService(INavigationService ns)
        {
            NavigationService = ns;
        }

        public PlayerWithPageContainerViewModel(HohoemaApp hohoemaApp, HohoemaPlaylist playlist)
        {
            HohoemaPlaylist = playlist;

            IsFillFloatContent = HohoemaPlaylist
                .ObserveProperty(x => x.IsPlayerFloatingModeEnable)
                .Select(x => !x)
                .ToReadOnlyReactiveProperty();

            IsFillFloatContent_DelayedRead = IsFillFloatContent
                .Delay(TimeSpan.FromMilliseconds(300))
                .ToReadOnlyReactiveProperty();


            IsVisibleFloatContent = HohoemaPlaylist.ObserveProperty(x => x.IsDisplayMainViewPlayer)
                .ToReactiveProperty(mode: ReactivePropertyMode.DistinctUntilChanged);

            IsContentDisplayFloating = Observable.CombineLatest(
                IsFillFloatContent.Select(x => !x),
                IsVisibleFloatContent
                )
                .Select(x => x.All(y => y))
                .ToReactiveProperty();


            HohoemaPlaylist.OpenPlaylistItem += HohoemaPlaylist_OpenPlaylistItem;

            IsVisibleFloatContent
                .Where(x => !x)
                .Subscribe(x =>
            {
                ClosePlayer();
            });

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                Observable.Merge(
                    IsVisibleFloatContent.Where(x => !x),
                    IsContentDisplayFloating.Where(x => x)
                    )
                    .Subscribe(async x =>
                    {
                        var view = ApplicationView.GetForCurrentView();
                        if (view.IsViewModeSupported(ApplicationViewMode.CompactOverlay))
                        {
                            var result = await view.TryEnterViewModeAsync(ApplicationViewMode.Default);
                            if (result)
                            {
                                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
                                view.TitleBar.ButtonBackgroundColor = null;
                                view.TitleBar.ButtonInactiveBackgroundColor = null;

                            }
                        }
                    });
            }
        }

        private async void HohoemaPlaylist_OpenPlaylistItem(IPlayableList playlist, PlaylistItem item)
        {
            await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ShowPlayer(item);
            });
        }

        private bool ShowPlayer(PlaylistItem item)
        {
            string pageType = null;
            string parameter = null;
            switch (item.Type)
            {
                case PlaylistItemType.Video:
                    pageType = nameof(Views.VideoPlayerPage);
                    parameter = new VideoPlayPayload()
                    {
                        VideoId = item.ContentId
                    }
                    .ToParameterString();
                    break;
                case PlaylistItemType.Live:
                    pageType = nameof(Views.LivePlayerPage);
                    parameter = new LiveVideoPagePayload(item.ContentId)
                    {
                        LiveTitle = item.Title,
                    }
                    .ToParameterString();
                    break;
                default:
                    break;
            }

            return NavigationService.Navigate(pageType, parameter);
        }

        private void ClosePlayer()
        {
            NavigationService.Navigate("Blank", null);
        }

        private DelegateCommand _PlayerFillDisplayCommand;
        public DelegateCommand TogglePlayerFloatDisplayCommand
        {
            get
            {
                return _PlayerFillDisplayCommand
                    ?? (_PlayerFillDisplayCommand = new DelegateCommand(() =>
                    {
                        // プレイヤー表示中だけ切り替えを受け付け
                        if (!HohoemaPlaylist.IsDisplayMainViewPlayer) { return; }

                        // メインウィンドウでの表示状態を「ウィンドウ全体」⇔「小窓表示」と切り替える
                        if (HohoemaPlaylist.PlayerDisplayType == PlayerDisplayType.PrimaryView)
                        {
                            HohoemaPlaylist.PlayerDisplayType = PlayerDisplayType.PrimaryWithSmall;
                        }
                        else if (HohoemaPlaylist.PlayerDisplayType == PlayerDisplayType.PrimaryWithSmall)
                        {
                            HohoemaPlaylist.PlayerDisplayType = PlayerDisplayType.PrimaryView;
                        }
                    }));
            }
        }


        private DelegateCommand _ClosePlayerCommand;
        public DelegateCommand ClosePlayerCommand
        {
            get
            {
                return _ClosePlayerCommand
                    ?? (_ClosePlayerCommand = new DelegateCommand(() =>
                    {
                        HohoemaPlaylist.IsDisplayMainViewPlayer = false;
                    }));
            }
        }
    }

}
