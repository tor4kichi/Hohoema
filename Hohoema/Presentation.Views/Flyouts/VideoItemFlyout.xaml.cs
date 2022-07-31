using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Hohoema.Models.Domain;
using Hohoema.Presentation.Services;
using Hohoema.Models.Domain.Player.Video.Cache;
using Windows.UI.Xaml.Controls.Primitives;
using Hohoema.Models.UseCase.Playlist;
using System.Collections.Generic;
using System.Linq;
using Hohoema.Presentation.Views.Helpers;
using I18NPortable;
using System;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Mylist.LoginUser;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.Subscriptions;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Presentation.ViewModels.Navigation.Commands;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Models.Domain.Niconico.Video.WatchHistory.LoginUser;
using Hohoema.Presentation.ViewModels.Niconico.Share;
using Hohoema.Presentation.ViewModels.VideoCache.Commands;
using Hohoema.Models.Domain.VideoCache;
using NiconicoToolkit.Video;
using Hohoema.Presentation.ViewModels.Niconico.Video;
using Hohoema.Models.Domain.LocalMylist;
using Hohoema.Models.UseCase.Hohoema.LocalMylist;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Presentation.ViewModels;

namespace Hohoema.Presentation.Views.Flyouts
{
    public sealed partial class VideoItemFlyout : MenuFlyout
    {
        public IPlaylist Playlist
        {
            get { return (IPlaylist)GetValue(PlaylistProperty); }
            set { SetValue(PlaylistProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Playlist.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlaylistProperty =
            DependencyProperty.Register("Playlist", typeof(IPlaylist), typeof(VideoItemFlyout), new PropertyMetadata(null));


        public IReadOnlyCollection<IVideoContent> SelectedVideoItems
        {
            get { return (IReadOnlyCollection<IVideoContent>)GetValue(SelectedVideoItemsProperty); }
            set { SetValue(SelectedVideoItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedVideoItemsProperty =
            DependencyProperty.Register(nameof(SelectedVideoItems), typeof(IReadOnlyCollection<IVideoContent>), typeof(VideoItemFlyout), new PropertyMetadata(null));


        public IReadOnlyCollection<IVideoContent> SourceVideoItems
        {
            get { return (IReadOnlyCollection<IVideoContent>)GetValue(SourceVideoItemsProperty); }
            set { SetValue(SourceVideoItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceVideoItemsProperty =
            DependencyProperty.Register(nameof(SourceVideoItems), typeof(IReadOnlyCollection<IVideoContent>), typeof(VideoItemFlyout), new PropertyMetadata(null));






        public bool AllowSelection { get; set; } = true;


        public static QueuePlaylist QueuePlaylist { get; }
        public static PageManager PageManager { get; }
        public static LoginUserOwnedMylistManager UserMylistManager { get; }
        public static LocalMylistManager LocalMylistManager { get; }
        public static SubscriptionManager SubscriptionManager { get; }
        public static VideoCacheManager VideoCacheManager { get; }
        public static VideoItemsSelectionContext VideoItemsSelectionContext { get; }
        public static VideoFilteringSettings VideoFilteringSettings { get; }

        private static readonly IMessenger _messenger;

        public static MylistCreateCommand CreateMylistCommand { get; }
        public static LocalPlaylistCreateCommand CreateLocalMylistCommand { get; }
        public static AddSubscriptionCommand AddSubscriptionCommand { get; }

        public static OpenLinkCommand OpenLinkCommand { get; }
        public static CopyToClipboardCommand CopyToClipboardCommand { get; }
        public static CopyToClipboardWithShareTextCommand CopyToClipboardWithShareTextCommand { get; }
        public static OpenShareUICommand OpenShareUICommand { get; }

        private string _localizedText_CreateNew { get; } =  "CreateNew".Translate();

        static VideoItemFlyout()
        {
            QueuePlaylist = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<QueuePlaylist>();
            _messenger = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<IMessenger>();
            CreateMylistCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<MylistCreateCommand>();
            CreateLocalMylistCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<LocalPlaylistCreateCommand>();
            PageManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<PageManager>();
            UserMylistManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<LoginUserOwnedMylistManager>();
            LocalMylistManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<LocalMylistManager>();
            SubscriptionManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<SubscriptionManager>();
            VideoCacheManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<VideoCacheManager>();
            VideoItemsSelectionContext = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<VideoItemsSelectionContext>();
            VideoFilteringSettings = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<VideoFilteringSettings>();

            OpenLinkCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<OpenLinkCommand>();
            CopyToClipboardCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<CopyToClipboardCommand>();
            CopyToClipboardWithShareTextCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<CopyToClipboardWithShareTextCommand>();
            OpenShareUICommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<OpenShareUICommand>();
        }


        public VideoItemFlyout()
        {
            this.InitializeComponent();

            SelectedVideoItems = new List<IVideoContent>();

            RemoveWatchHisotryItem.Command = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<WatchHistoryRemoveItemCommand>();
            AddWatchAfter.Command = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<QueueAddItemCommand>();
            RemoveWatchAfter.Command = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<QueueRemoveItemCommand>();

            OpenVideoInfoPage.Command = PageManager.OpenPageCommand;
            OpenOwnerMylistsPage.Command = new OpenPageWithIdCommand(HohoemaPageType.UserMylist, PageManager);
            OpenOwnerVideosPage.Command = PageManager.OpenVideoListPageCommand;
            OpenOwnerSeriesPage.Command = new OpenPageWithIdCommand(HohoemaPageType.UserSeries, PageManager);
            Share.Command = OpenShareUICommand;
            CopyVideoId.Command = CopyToClipboardCommand;
            CopyVideoLink.Command = CopyToClipboardCommand;
            CopyShareText.Command = CopyToClipboardWithShareTextCommand;

            LocalMylistItem.Command = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<LocalPlaylistAddItemCommand>();
            AddToMylistItem.Command = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<MylistAddItemCommand>();

            AddSusbcriptionItem.Command = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<AddSubscriptionCommand>();

            CacheRequest.Command = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<CacheAddRequestCommand>();
            DeleteCacheRequest.Command = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<CacheDeleteRequestCommand>();

            AddNgUser.Command = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<HiddenVideoOwnerAddCommand>();
            RemoveNgUser.Command = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<HiddenVideoOwnerRemoveCommand>();

            Opening += VideoItemFlyout_Opening;
        }

        private void VideoItemFlyout_Opening(object sender, object e)
        {
            object dataContext = Target.DataContext ?? (Target as SelectorItem)?.Content;
            var content = (dataContext as IVideoContent);

            if (content == null || (SelectedVideoItems?.Any() ?? false))
            {
                content = SelectedVideoItems?.First();
                dataContext = SelectedVideoItems;
            }

            bool isMultipleSelection = AllowSelection && SelectedVideoItems?.Count >= 2;

            var playlist = Playlist;


            // 視聴履歴
            RemoveWatchHisotryItem.Visibility = (content is IWatchHistory).ToVisibility();

            // ローカルプレイリスト
            if (playlist is LocalPlaylist localPlaylist)
            {
                RemoveLocalPlaylistItem.CommandParameter = dataContext;
                RemoveLocalPlaylistItem.Command = new LocalPlaylistRemoveItemCommand(localPlaylist);
                RemoveLocalPlaylistItem.Visibility = Visibility.Visible;
            }
            else
            {
                RemoveLocalPlaylistItem.Visibility = Visibility.Collapsed;
            }

            // マイリスト
            if (playlist is LoginUserMylistPlaylist mylistPlaylist)
            {
                RemoveMylistItem.CommandParameter = dataContext;
                RemoveMylistItem.Command = new MylistRemoveItemCommand(mylistPlaylist);
                RemoveMylistItem.Visibility = Visibility.Visible;
            }
            else
            {
                RemoveMylistItem.Visibility = Visibility.Collapsed;
            }


            // あとで見る
            AddWatchAfter.CommandParameter = dataContext;
            RemoveWatchAfter.CommandParameter = dataContext;
            if (isMultipleSelection)
            {
                AddWatchAfter.Visibility = SelectedVideoItems.All(x => QueuePlaylist.Contains(x.VideoId)).ToInvisibility();
                RemoveWatchAfter.Visibility = SelectedVideoItems.Any(x => QueuePlaylist.Contains(x.VideoId)).ToVisibility();
            }
            else if (QueuePlaylist.Contains(content.VideoId))
            {
                AddWatchAfter.Visibility = Visibility.Collapsed;
                RemoveWatchAfter.Visibility = Visibility.Visible;
            }
            else
            {
                AddWatchAfter.Visibility = Visibility.Visible;
                RemoveWatchAfter.Visibility = Visibility.Collapsed;
            }


            // ここから連続再生
            if (!isMultipleSelection 
                && content is not null
                && playlist is not null
                )
            {
                PlaylistPlayFromHere.Visibility = Visibility.Visible;
                PlaylistPlayFromHere.Command = new PlaylistPlayFromHereCommand(playlist, _messenger);
                PlaylistPlayFromHere.CommandParameter = content;
            }
            else
            {
                PlaylistPlayFromHere.Visibility = Visibility.Collapsed;
            }

            // マイリスト
            AddToMylistItem.Visibility = UserMylistManager.IsLoginUserMylistReady ? Visibility.Visible : Visibility.Collapsed;
            AddToMylistItem.CommandParameter = dataContext;


            if (isMultipleSelection is false)
            {
                OpenVideoInfoPage.Visibility = Visibility.Visible;
                VideoInfoItemSeparator.Visibility = Visibility.Visible;
                ExternalActionsSeparator.Visibility = Visibility.Visible;

                if (content is IVideoContentProvider provider && provider.ProviderId != null)
                {
                    OpenOwnerVideosPage.Visibility = Visibility.Visible;

                    bool isUserProvidedVideo = (provider.ProviderType == OwnerType.User && provider.ProviderId != null);
                    OpenOwnerMylistsPage.Visibility =
                    OpenOwnerSeriesPage.Visibility = isUserProvidedVideo.ToVisibility();

                    OpenOwnerMylistsPage.CommandParameter =
                    OpenOwnerSeriesPage.CommandParameter = provider?.ProviderId;
                    
                    AddSusbcriptionItem.CommandParameter = provider;
                    AddSusbcriptionItem.Visibility = Visibility.Visible;
                }
                else
                {
                    OpenOwnerVideosPage.Visibility = Visibility.Collapsed;
                    OpenOwnerMylistsPage.Visibility = Visibility.Collapsed;
                    OpenOwnerSeriesPage.Visibility = Visibility.Collapsed;
                    AddSusbcriptionItem.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                OpenVideoInfoPage.Visibility = Visibility.Collapsed;
                VideoInfoItemSeparator.Visibility = Visibility.Collapsed;
                ExternalActionsSeparator.Visibility = Visibility.Collapsed;

                OpenOwnerVideosPage.Visibility = Visibility.Collapsed;
                OpenOwnerMylistsPage.Visibility = Visibility.Collapsed;
                OpenOwnerSeriesPage.Visibility = Visibility.Collapsed;
                AddSusbcriptionItem.Visibility = Visibility.Collapsed;
            }

            var visibleSingleSelectionItem = isMultipleSelection.ToInvisibility();
            Share.Visibility = visibleSingleSelectionItem;
            CopySubItem.Visibility = visibleSingleSelectionItem;

            // プレイリスト
            LocalMylistItem.CommandParameter = dataContext;
            
            // NG投稿者
            if (VideoFilteringSettings.NGVideoOwnerUserIdEnable)
            {
                AddNgUser.Visibility = AddNgUser.Command.CanExecute(content).ToVisibility();
                RemoveNgUser.Visibility = RemoveNgUser.Command.CanExecute(content).ToVisibility();
            }
            else
            {
                AddNgUser.Visibility = Visibility.Collapsed;
                RemoveNgUser.Visibility = Visibility.Collapsed;
            }

            // キャッシュ
            var canNewDownloadCache = VideoCacheManager.IsCacheDownloadAuthorized();
            var canNewDownloadCacheToVisibility = canNewDownloadCache.ToVisibility();
            if (isMultipleSelection)
            {
                // 一つでもキャッシュ済みがあれば削除ボタンを表示
                // 一つでも未キャッシュがあれば取得ボタンを表示
                var anyItemsCached = SelectedVideoItems.Any(x => VideoCacheManager.GetVideoCacheStatus(x.VideoId) is not null);
                var anyItemsNotCached = SelectedVideoItems.Any(x => VideoCacheManager.GetVideoCacheStatus(x.VideoId) is null or VideoCacheStatus.DownloadPaused or VideoCacheStatus.Downloading or VideoCacheStatus.Failed);

                var notCachedToVisible = (canNewDownloadCache && anyItemsNotCached).ToVisibility();
                CacheRequest.Visibility = notCachedToVisible;
                CacheRequest.CommandParameter = dataContext;
                (CacheRequest.Command as CommandBase).NotifyCanExecuteChanged();

                CacheRequestWithQuality.Visibility = notCachedToVisible;
                DeleteCacheRequest.CommandParameter = dataContext;
                (DeleteCacheRequest.Command as CommandBase).NotifyCanExecuteChanged();

                var cachedToVisible = (anyItemsCached).ToVisibility();
                DeleteCacheRequest.Visibility = cachedToVisible;

                CacheSeparator.Visibility = ((RemoveNgUser.Visibility == Visibility.Visible || AddNgUser.Visibility == Visibility.Visible) && 
                    notCachedToVisible is Visibility.Visible || cachedToVisible is Visibility.Visible).ToVisibility();
            }
            else
            {
                var itemCached = VideoCacheManager.GetVideoCacheStatus(content.VideoId) is not null;
                var itemNotCached = VideoCacheManager.GetVideoCacheStatus(content.VideoId) is null or VideoCacheStatus.DownloadPaused or VideoCacheStatus.Downloading or VideoCacheStatus.Failed;

                var notCachedToVisible = (canNewDownloadCache && itemNotCached).ToVisibility();
                CacheRequest.Visibility = notCachedToVisible;
                CacheRequest.CommandParameter = dataContext;
                CacheRequestWithQuality.Visibility = notCachedToVisible;
                DeleteCacheRequest.CommandParameter = dataContext;

                var cachedToVisible = (itemCached).ToVisibility();
                DeleteCacheRequest.Visibility = cachedToVisible;

                CacheSeparator.Visibility = ((RemoveNgUser.Visibility == Visibility.Visible || AddNgUser.Visibility == Visibility.Visible) && 
                    notCachedToVisible is Visibility.Visible || cachedToVisible is Visibility.Visible).ToVisibility();
            }


            if (CacheRequestWithQuality.Items.Count == 0)
            {
                foreach (var quality in Enum.GetValues(typeof(NicoVideoQuality)).Cast<NicoVideoQuality>().Where(x => x != NicoVideoQuality.Unknown))
                {
                    var command = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<CacheAddRequestCommand>();
                    command.VideoQuality = quality;
                    var cacheRequestMenuItem = new MenuFlyoutItem() 
                    {
                        Text = quality.Translate(),
                        Command = command,
                    };
                    CacheRequestWithQuality.Items.Add(cacheRequestMenuItem);
                }
            }

            foreach (var qualityCacheRequest in CacheRequestWithQuality.Items)
            {
                (qualityCacheRequest as MenuFlyoutItem).CommandParameter = dataContext;
            }
        }
    }
}
