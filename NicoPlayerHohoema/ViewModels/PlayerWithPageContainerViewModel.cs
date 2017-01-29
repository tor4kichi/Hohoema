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

namespace NicoPlayerHohoema.ViewModels
{
    public class PlayerWithPageContainerViewModel : BindableBase
    {
        public HohoemaPlaylist HohoemaPlaylist { get; private set; }

        public ReactiveProperty<bool> IsFillFloatContent { get; private set; }
        public ReactiveProperty<bool> IsVisibleFloatContent { get; private set; }

        private Dictionary<string, object> viewModelState = new Dictionary<string, object>();

        // TODO: プレイヤーのVMを管理する
        
        // 動画または生放送のVM
        public ReactiveProperty<ViewModelBase> ContentVM { get; private set; }

        public PlayerWithPageContainerViewModel(HohoemaPlaylist playlist)
        {
            HohoemaPlaylist = playlist;

            IsFillFloatContent = HohoemaPlaylist.ObserveProperty(x => x.IsPlayerFloatingModeEnable)
                .Select(x => !x)
                .ToReactiveProperty();

            IsVisibleFloatContent = HohoemaPlaylist.ObserveProperty(x => x.IsDisplayPlayer)
                .ToReactiveProperty();

            ContentVM = new ReactiveProperty<ViewModelBase>();


            HohoemaPlaylist.OpenPlaylistItem += HohoemaPlaylist_OpenPlaylistItem;

            IsVisibleFloatContent
                .Where(x => !x)
                .Subscribe(x => 
            {
                ClosePlayer();
            });

        }

        private void Nav_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (IsFillFloatContent.Value)
            {
                ClosePlayer();
            }
        }

        private void HohoemaPlaylist_OpenPlaylistItem(Playlist playlist, PlaylistItem item)
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
                ContentVM.Value = null;
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
}
