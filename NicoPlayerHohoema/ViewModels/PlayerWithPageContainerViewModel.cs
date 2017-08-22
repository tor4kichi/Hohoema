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

namespace NicoPlayerHohoema.ViewModels
{
    public class PlayerWithPageContainerViewModel : BindableBase
    {
        public HohoemaPlaylist HohoemaPlaylist { get; private set; }

        public ReactiveProperty<bool> IsFillFloatContent { get; private set; }
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

        

        // 動画または生放送のVM
        public ReactiveProperty<ViewModelBase> ContentVM { get; private set; }

        public PlayerWithPageContainerViewModel(HohoemaApp hohoemaApp, HohoemaPlaylist playlist)
        {
            HohoemaPlaylist = playlist;

            IsFillFloatContent = HohoemaPlaylist
                .ToReactivePropertyAsSynchronized(x => 
                    x.IsPlayerFloatingModeEnable
                    , (x) => !x
                    , (x) => !x
                    );

            IsFillFloatContent_DelayedRead = IsFillFloatContent
                .Delay(TimeSpan.FromMilliseconds(300))
                .ToReadOnlyReactiveProperty();


            IsVisibleFloatContent = HohoemaPlaylist.ObserveProperty(x => x.IsDisplayPlayer)
                .ToReactiveProperty();

            IsContentDisplayFloating = Observable.CombineLatest(
                IsFillFloatContent.Select(x => !x),
                IsVisibleFloatContent
                )
                .Select(x => x.All(y => y))
                .ToReactiveProperty();
                
            ContentVM = new ReactiveProperty<ViewModelBase>();


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
                            }
                        }
                    });
            }

            App.Current.Suspending += Current_Suspending;
        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            ContentVM.Value?.OnNavigatingFrom(new Prism.Windows.Navigation.NavigatingFromEventArgs()
            {
                NavigationMode = NavigationMode.New,
            }, viewModelState, true);
        }

        private void HohoemaPlaylist_OpenPlaylistItem(IPlayableList playlist, PlaylistItem item)
        {
            // TODO: 別ウィンドウでプレイヤーを表示している場合に処理をキャンセル
            ClosePlayer();

            ViewModelBase newPlayerVM = null;
            string parameter = null;
            switch (item.Type)
            {
                case PlaylistItemType.Video:
                    newPlayerVM = App.Current.Container.Resolve<VideoPlayerControlViewModel>();
                    parameter = new VideoPlayPayload()
                    {
                        VideoId = item.ContentId
                    }
                    .ToParameterString();
                    break;
                case PlaylistItemType.Live:
                    newPlayerVM = App.Current.Container.Resolve<LiveVideoPlayerControlViewModel>();
                    parameter = new LiveVideoPagePayload(item.ContentId)
                    {
                        LiveTitle = item.Title,
                    }
                    .ToParameterString();
                    break;
                default:
                    break;
            }

            if (newPlayerVM != null)
            {
                newPlayerVM.OnNavigatedTo(
                    new Prism.Windows.Navigation.NavigatedToEventArgs()
                    {
                        NavigationMode = NavigationMode.New,
                        Parameter = parameter
                    }, viewModelState);

                ContentVM.Value = newPlayerVM;
            }
        }


        private void ClosePlayer()
        {
            if (ContentVM.Value != null)
            {
                var oldContent = ContentVM.Value;
                oldContent.OnNavigatingFrom(new Prism.Windows.Navigation.NavigatingFromEventArgs()
                {
                    NavigationMode = NavigationMode.New,
                }, viewModelState, false);
                ContentVM.Value = new EmptyContentViewModel();
                (oldContent as IDisposable)?.Dispose();
            }

        }

        private DelegateCommand _PlayerFillDisplayCommand;
        public DelegateCommand PlayerFillDisplayCommand
        {
            get
            {
                return _PlayerFillDisplayCommand
                    ?? (_PlayerFillDisplayCommand = new DelegateCommand(() => 
                    {
                        HohoemaPlaylist.IsPlayerFloatingModeEnable = false;
                    }));
            }
        }

        private DelegateCommand _PlayerFloatDisplayCommand;
        public DelegateCommand PlayerFloatDisplayCommand
        {
            get
            {
                return _PlayerFloatDisplayCommand
                    ?? (_PlayerFloatDisplayCommand = new DelegateCommand(() =>
                    {

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
                        HohoemaPlaylist.IsDisplayPlayer = false;
                    }));
            }
        }
    }


    public class EmptyContentViewModel : ViewModelBase
    {

    }
}
