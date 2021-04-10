using Windows.UI.Xaml.Controls;
using Unity;
using Windows.UI.Xaml;
using Prism.Ioc;
using Hohoema.Models.Domain;
using Hohoema.Presentation.Services;
using Hohoema.Models.Domain.Player.Video.Cache;
using Windows.UI.Xaml.Controls.Primitives;
using Hohoema.Models.UseCase.NicoVideos;
using System.Collections.Generic;
using System.Linq;
using Hohoema.Presentation.Views.Helpers;
using I18NPortable;
using System;
using Prism.Commands;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Uno.Extensions.Specialized;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using Hohoema.Presentation.Services.Page;
using Hohoema.Presentation.ViewModels.Subscriptions;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Presentation.ViewModels.Navigation.Commands;
using Hohoema.Presentation.ViewModels.NicoVideos.Commands;
using Hohoema.Presentation.ViewModels.Subscriptions.Commands;

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



        public static HohoemaPlaylist HohoemaPlaylist { get; }
        public static ExternalAccessService ExternalAccessService { get; }
        public static PageManager PageManager { get; }
        public static UserMylistManager UserMylistManager { get; }
        public static LocalMylistManager LocalMylistManager { get; }
        public static SubscriptionManager SubscriptionManager { get; }
        public static VideoCacheManagerLegacy VideoCacheManager { get; }
        public static VideoItemsSelectionContext VideoItemsSelectionContext { get; }
        public static MylistCreateCommand CreateMylistCommand { get; }
        public static LocalPlaylistCreateCommand CreateLocalMylistCommand { get; }
        public static AddSubscriptionCommand AddSubscriptionCommand { get; }



        private string _localizedText_CreateNew { get; } =  "CreateNew".Translate();

        static VideoItemFlyout()
        {
            CreateMylistCommand = App.Current.Container.Resolve<MylistCreateCommand>();
            CreateLocalMylistCommand = App.Current.Container.Resolve<LocalPlaylistCreateCommand>();
            HohoemaPlaylist = App.Current.Container.Resolve<HohoemaPlaylist>();
            ExternalAccessService = App.Current.Container.Resolve<ExternalAccessService>();
            PageManager = App.Current.Container.Resolve<PageManager>();
            UserMylistManager = App.Current.Container.Resolve<UserMylistManager>();
            LocalMylistManager = App.Current.Container.Resolve<LocalMylistManager>();
            SubscriptionManager = App.Current.Container.Resolve<SubscriptionManager>();
            VideoCacheManager = App.Current.Container.Resolve<VideoCacheManagerLegacy>();
            VideoItemsSelectionContext = App.Current.Container.Resolve<VideoItemsSelectionContext>();
        }


        public VideoItemFlyout()
        {
            this.InitializeComponent();

            SelectedVideoItems = new List<IVideoContent>();

            RemoveWatchHisotryItem.Command = App.Current.Container.Resolve<WatchHistoryRemoveItemCommand>();
            AddWatchAfter.Command = App.Current.Container.Resolve<QueueAddItemCommand>();
            RemoveWatchAfter.Command = App.Current.Container.Resolve<QueueRemoveItemCommand>();

            OpenVideoInfoPage.Command = PageManager.OpenPageCommand;
            OpenOwnerMylistsPage.Command = new OpenPageWithIdCommand(HohoemaPageType.UserMylist, PageManager);
            OpenOwnerVideosPage.Command = PageManager.OpenVideoListPageCommand;
            OpenOwnerSeriesPage.Command = new OpenPageWithIdCommand(HohoemaPageType.UserSeries, PageManager);
            Share.Command = ExternalAccessService.OpenShareUICommand;
            CopyVideoId.Command = ExternalAccessService.CopyToClipboardCommand;
            CopyVideoLink.Command = ExternalAccessService.CopyToClipboardCommand;
            CopyShareText.Command = ExternalAccessService.CopyToClipboardWithShareTextCommand;

            LocalMylistItem.Command = App.Current.Container.Resolve<LocalPlaylistAddItemCommand>();
            AddToMylistItem.Command = App.Current.Container.Resolve<MylistAddItemCommand>();

            AddSusbcriptionItem.Command = App.Current.Container.Resolve<AddSubscriptionCommand>();

            CacheRequest.Command = App.Current.Container.Resolve<CacheAddRequestCommand>();
            DeleteCacheRequest.Command = App.Current.Container.Resolve<CacheDeleteRequestCommand>();

            AddNgUser.Command = App.Current.Container.Resolve<HiddenVideoOwnerAddCommand>();
            RemoveNgUser.Command = App.Current.Container.Resolve<HiddenVideoOwnerRemoveCommand>();

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
                AddWatchAfter.Visibility = SelectedVideoItems.All(x => HohoemaPlaylist.QueuePlaylist.Contains(x)).ToInvisibility();
                RemoveWatchAfter.Visibility = SelectedVideoItems.Any(x => HohoemaPlaylist.QueuePlaylist.Contains(x)).ToVisibility();
            }
            else if (HohoemaPlaylist.QueuePlaylist.Contains(content))
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
            if ((SourceVideoItems?.Any() ?? false)
                && !isMultipleSelection 
                && Playlist?.Id == HohoemaPlaylist.QueuePlaylistId
                )
            {
                AllPlayFromHereWithWatchAfter.Visibility = Visibility.Visible;
                AllPlayFromHereWithWatchAfter.Command = new DelegateCommand<IVideoContent>((param) =>
                {
                    var index = SourceVideoItems.IndexOf(param);
                    if (index < 0) { return; }
                    var items = SourceVideoItems.Take(index + 1).ToList();
                    if (items.Count >= 2)
                    {
                        foreach (var videoItem in items)
                        {
                            HohoemaPlaylist.AddQueuePlaylist(videoItem, ContentInsertPosition.Head);
                        }
                    }

                    if (items.Count >= 1)
                    {
                        HohoemaPlaylist.Play(items.Last(), HohoemaPlaylist.QueuePlaylist);
                    }
                });
                AllPlayFromHereWithWatchAfter.CommandParameter = dataContext;

            }
            else
            {
                AllPlayFromHereWithWatchAfter.Visibility = Visibility.Collapsed;
            }



            // マイリスト
            AddToMylistItem.Visibility = UserMylistManager.IsLoginUserMylistReady ? Visibility.Visible : Visibility.Collapsed;
            AddToMylistItem.CommandParameter = dataContext;


            var visibleSingleSelectionItem = isMultipleSelection.ToInvisibility();
            OpenVideoInfoPage.Visibility = visibleSingleSelectionItem;
            OpenOwnerVideosPage.Visibility = visibleSingleSelectionItem;
            AddNgUser.Visibility = visibleSingleSelectionItem;
            VideoInfoItemSeparator.Visibility = visibleSingleSelectionItem;
            ExternalActionsSeparator.Visibility = visibleSingleSelectionItem;

            if (!isMultipleSelection)
            {
                bool isUserProvidedVideo = (content?.ProviderType == NicoVideoUserType.User && content?.ProviderId != null);
                OpenOwnerMylistsPage.Visibility = 
                OpenOwnerSeriesPage.Visibility = isUserProvidedVideo.ToVisibility();

                OpenOwnerMylistsPage.CommandParameter =
                OpenOwnerSeriesPage.CommandParameter = content?.ProviderId;
            }
            else
            {
                OpenOwnerMylistsPage.Visibility = Visibility.Collapsed;
                OpenOwnerSeriesPage.Visibility = Visibility.Collapsed;
            }

            Share.Visibility = visibleSingleSelectionItem;
            CopySubItem.Visibility = visibleSingleSelectionItem;

            AddSusbcriptionItem.Visibility = visibleSingleSelectionItem;


            // プレイリスト
            LocalMylistItem.CommandParameter = dataContext;
            

            // NG投稿者
            AddNgUser.Visibility = AddNgUser.Command.CanExecute(content).ToVisibility();
            RemoveNgUser.Visibility = RemoveNgUser.Command.CanExecute(content).ToVisibility();


            // キャッシュ
            var isCacheEnabled = VideoCacheManager.CacheSettings.IsEnableCache && VideoCacheManager.CacheSettings.IsUserAcceptedCache;
            var cacheEnableToVisibility = isCacheEnabled.ToVisibility();
            if (isMultipleSelection && isCacheEnabled)
            {
                // 一つでもキャッシュ済みがあれば削除ボタンを表示
                // 一つでも未キャッシュがあれば取得ボタンを表示
                var anyItemsCached = SelectedVideoItems.Any(x => VideoCacheManager.IsCacheRequested(x.Id));
                var anyItemsNotCached = SelectedVideoItems.Any(x => !VideoCacheManager.CheckCachedAsyncUnsafe(x.Id));

                var notCachedToVisible = (anyItemsNotCached).ToVisibility();
                CacheRequest.Visibility = notCachedToVisible;
                CacheRequest.CommandParameter = dataContext;
                (CacheRequest.Command as DelegateCommandBase).RaiseCanExecuteChanged();

                CacheRequestWithQuality.Visibility = notCachedToVisible;
                DeleteCacheRequest.CommandParameter = dataContext;
                (DeleteCacheRequest.Command as DelegateCommandBase).RaiseCanExecuteChanged();
                CacheSeparator.Visibility = Visibility.Visible;

                var cachedToVisible = (anyItemsCached).ToVisibility();
                DeleteCacheRequest.Visibility = cachedToVisible;
            }
            else if (isCacheEnabled)
            {
                var itemCached = VideoCacheManager.IsCacheRequested(content.Id);
                var itemNotCached = !VideoCacheManager.CheckCachedAsyncUnsafe(content.Id);

                var notCachedToVisible = (itemNotCached).ToVisibility();
                CacheRequest.Visibility = notCachedToVisible;
                CacheRequest.CommandParameter = dataContext;
                CacheRequestWithQuality.Visibility = notCachedToVisible;
                DeleteCacheRequest.CommandParameter = dataContext;

                CacheSeparator.Visibility = Visibility.Visible;

                var cachedToVisible = (itemCached).ToVisibility();
                DeleteCacheRequest.Visibility = cachedToVisible;
            }
            else
            {
                CacheRequest.Visibility = Visibility.Collapsed;
                CacheRequestWithQuality.Visibility = Visibility.Collapsed;
                CacheSeparator.Visibility = Visibility.Collapsed;
                DeleteCacheRequest.Visibility = Visibility.Collapsed;
            }
            

            if (CacheRequestWithQuality.Items.Count == 0)
            {
                foreach (var quality in Enum.GetValues(typeof(NicoVideoQuality)).Cast<NicoVideoQuality>().Where(x => x.IsDmc() && x != NicoVideoQuality.Unknown))
                {
                    var command = App.Current.Container.Resolve<CacheAddRequestCommand>();
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
