using Windows.UI.Xaml.Controls;
using Unity;
using Windows.UI.Xaml;
using Prism.Ioc;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Commands.Mylist;
using NicoPlayerHohoema.Commands;
using System.Reactive.Concurrency;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Models.LocalMylist;
using NicoPlayerHohoema.Models.Subscription;
using NicoPlayerHohoema.Models.Cache;
using NicoPlayerHohoema.Models.Provider;
using Windows.UI.Xaml.Controls.Primitives;
using System.Globalization;
using NicoPlayerHohoema.UseCase.Playlist;
using NicoPlayerHohoema.UseCase.Playlist.Commands;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Repository.Playlist;
using System.Collections.Generic;
using System.Linq;
using NicoPlayerHohoema.Views.Helpers;
using I18NPortable;
using NicoPlayerHohoema.UseCase.Page.Commands;
using System;
using Prism.Commands;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Uno.Extensions.Specialized;

namespace NicoPlayerHohoema.Views.Flyouts
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


        public IReadOnlyCollection<Interfaces.IVideoContent> SelectedVideoItems
        {
            get { return (IReadOnlyCollection<Interfaces.IVideoContent>)GetValue(SelectedVideoItemsProperty); }
            set { SetValue(SelectedVideoItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedVideoItemsProperty =
            DependencyProperty.Register(nameof(SelectedVideoItems), typeof(IReadOnlyCollection<Interfaces.IVideoContent>), typeof(VideoItemFlyout), new PropertyMetadata(null));


        public IReadOnlyCollection<Interfaces.IVideoContent> SourceVideoItems
        {
            get { return (IReadOnlyCollection<Interfaces.IVideoContent>)GetValue(SourceVideoItemsProperty); }
            set { SetValue(SourceVideoItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceVideoItemsProperty =
            DependencyProperty.Register(nameof(SourceVideoItems), typeof(IReadOnlyCollection<Interfaces.IVideoContent>), typeof(VideoItemFlyout), new PropertyMetadata(null));






        public bool AllowSelection { get; set; } = true;



        public HohoemaPlaylist HohoemaPlaylist { get; }
        public ExternalAccessService ExternalAccessService { get; }
        public PageManager PageManager { get; }
        public UserMylistManager UserMylistManager { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public SubscriptionManager SubscriptionManager { get; }
        public VideoCacheManager VideoCacheManager { get; }
        public VideoItemsSelectionContext VideoItemsSelectionContext { get; }
        public Commands.Mylist.CreateMylistCommand CreateMylistCommand { get; }
        public Commands.Mylist.CreateLocalMylistCommand CreateLocalMylistCommand { get; }
        public ViewModels.Subscriptions.AddSubscriptionCommand AddSubscriptionCommand { get; }



        private string _localizedText_CreateNew { get; } =  "CreateNew".Translate();

        public VideoItemFlyout()
        {
            this.InitializeComponent();

            SelectedVideoItems = new List<IVideoContent>();

            CreateMylistCommand = App.Current.Container.Resolve<CreateMylistCommand>();
            CreateLocalMylistCommand = App.Current.Container.Resolve<CreateLocalMylistCommand>();
            HohoemaPlaylist = App.Current.Container.Resolve<HohoemaPlaylist>();
            ExternalAccessService = App.Current.Container.Resolve<ExternalAccessService>();
            PageManager = App.Current.Container.Resolve<PageManager>();
            UserMylistManager = App.Current.Container.Resolve<UserMylistManager>();
            LocalMylistManager = App.Current.Container.Resolve<LocalMylistManager>();
            SubscriptionManager = App.Current.Container.Resolve<SubscriptionManager>();
            VideoCacheManager = App.Current.Container.Resolve<VideoCacheManager>();
            VideoItemsSelectionContext = App.Current.Container.Resolve<VideoItemsSelectionContext>();

            RemoveWatchHisotryItem.Command = App.Current.Container.Resolve<WatchHistoryRemoveItemCommand>();
            AddWatchAfter.Command = App.Current.Container.Resolve<WatchAfterAddItemCommand>();
            RemoveWatchAfter.Command = App.Current.Container.Resolve<WatchAfterRemoveItemCommand>();

            OpenVideoInfoPage.Command = PageManager.OpenPageCommand;
            OpenOwnerVideosPage.Command = PageManager.OpenVideoListPageCommand;
            OpenOwnerSeriesPage.Command = new OpenPageWithIdCommand(HohoemaPageType.UserSeries, PageManager);
            Share.Command = ExternalAccessService.OpenShareUICommand;
            CopyVideoId.Command = ExternalAccessService.CopyToClipboardCommand;
            CopyVideoLink.Command = ExternalAccessService.CopyToClipboardCommand;
            CopyShareText.Command = ExternalAccessService.CopyToClipboardWithShareTextCommand;

            AddSusbcriptionItem.Command = App.Current.Container.Resolve<ViewModels.Subscriptions.AddSubscriptionCommand>();

            CacheRequest.Command = App.Current.Container.Resolve<CacheAddRequestCommand>();
            DeleteCacheRequest.Command = App.Current.Container.Resolve<CacheDeleteRequestCommand>();

            AddNgUser.Command = App.Current.Container.Resolve<AddToHiddenUserCommand>();
            RemoveNgUser.Command = App.Current.Container.Resolve<RemoveHiddenVideoOwnerCommand>();
            SelectionStart.Command = App.Current.Container.Resolve<SelectionStartCommand>();
            SelectionEnd.Command = App.Current.Container.Resolve<SelectionExitCommand>();
            SelectionAll.Command = App.Current.Container.Resolve<SelectionAllSelectCommand>();

            Opening += VideoItemFlyout_Opening;
        }

        private void VideoItemFlyout_Opening(object sender, object e)
        {
            object dataContext = Target.DataContext ?? (Target as SelectorItem)?.Content;
            var content = (dataContext as Interfaces.IVideoContent);

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
                RemoveLocalPlaylistItem.Command = localPlaylist.ItemsRemoveCommand;
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
                RemoveMylistItem.Command = mylistPlaylist.ItemsRemoveCommand;
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
                AddWatchAfter.Visibility = SelectedVideoItems.All(x => HohoemaPlaylist.WatchAfterPlaylist.Contains(x)).ToInvisibility();
                RemoveWatchAfter.Visibility = SelectedVideoItems.Any(x => HohoemaPlaylist.WatchAfterPlaylist.Contains(x)).ToVisibility();
            }
            else if (HohoemaPlaylist.WatchAfterPlaylist.Contains(content))
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
                && Playlist?.Id == HohoemaPlaylist.WatchAfterPlaylistId
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
                            HohoemaPlaylist.AddWatchAfterPlaylist(videoItem, ContentInsertPosition.Head);
                        }
                    }

                    if (items.Count >= 1)
                    {
                        HohoemaPlaylist.Play(items.Last(), HohoemaPlaylist.WatchAfterPlaylist);
                    }
                });
                AllPlayFromHereWithWatchAfter.CommandParameter = dataContext;

            }
            else
            {
                AllPlayFromHereWithWatchAfter.Visibility = Visibility.Collapsed;
            }



            // マイリスト
            AddToMylistSubItem.Items.Clear();
            AddToMylistSubItem.Visibility = UserMylistManager.IsLoginUserMylistReady ? Visibility.Visible : Visibility.Collapsed;
            if (UserMylistManager.IsLoginUserMylistReady)
            {
                AddToMylistSubItem.Items.Add(new MenuFlyoutItem()
                {
                    Text = _localizedText_CreateNew,
                    Command = CreateMylistCommand,
                    CommandParameter = dataContext
                });

                foreach (var mylist in UserMylistManager.Mylists)
                {
                    AddToMylistSubItem.Items.Add(new MenuFlyoutItem()
                    {
                        Text = mylist.Label,
                        Command = mylist.ItemsAddCommand,
                        CommandParameter = dataContext
                    });
                }
            }

            var visibleSingleSelectionItem = isMultipleSelection.ToInvisibility();
            OpenVideoInfoPage.Visibility = visibleSingleSelectionItem;
            OpenOwnerVideosPage.Visibility = visibleSingleSelectionItem;
            AddNgUser.Visibility = visibleSingleSelectionItem;
            VideoInfoItemSeparator.Visibility = visibleSingleSelectionItem;


            //OpenOwnerSeriesPage.Visibility = (content?.ProviderType == Database.NicoVideoUserType.User && content?.ProviderId != null).ToVisibility();
            //OpenOwnerSeriesPage.CommandParameter = content?.ProviderId;

            Share.Visibility = visibleSingleSelectionItem;
            CopySubItem.Visibility = visibleSingleSelectionItem;

            AddSusbcriptionItem.Visibility = visibleSingleSelectionItem;


            // プレイリスト
            LocalMylistSubItem.Items.Clear();
            LocalMylistSubItem.Items.Add(new MenuFlyoutItem()
            {
                Text = _localizedText_CreateNew,
                Command = CreateLocalMylistCommand,
                CommandParameter = dataContext
            });

            foreach (var localMylist in LocalMylistManager.LocalPlaylists)
            {
                LocalMylistSubItem.Items.Add(new MenuFlyoutItem()
                {
                    Text = localMylist.Label,
                    Command = localMylist.ItemsAddCommand,
                    CommandParameter = dataContext
                });
            }

            // 購読
            var susbcSourceConverter = new Subscriptions.SubscriptionSourceConverter();
            var subscSource = susbcSourceConverter.Convert(content, typeof(SubscriptionSource), null, null);
            
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
                CacheRequestWithQuality.Visibility = notCachedToVisible;
                DeleteCacheRequest.CommandParameter = dataContext;

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


            // 選択
            if (!AllowSelection)
            {
                SelectionSeparator.Visibility = Visibility.Collapsed;
                SelectionStart.Visibility = Visibility.Collapsed;
                SelectionEnd.Visibility = Visibility.Collapsed;
                SelectionAll.Visibility = Visibility.Collapsed;
            }
            else if (!VideoItemsSelectionContext.IsSelectionEnabled)
            {
                SelectionSeparator.Visibility = Visibility.Visible;
                SelectionStart.Visibility = Visibility.Visible;
                SelectionEnd.Visibility = Visibility.Collapsed;
                SelectionAll.Visibility = Visibility.Collapsed;
            }
            else
            {
                SelectionSeparator.Visibility = Visibility.Visible;
                SelectionStart.Visibility = Visibility.Collapsed;
                SelectionEnd.Visibility = Visibility.Visible;
                SelectionAll.Visibility = Visibility.Visible;
            }
        }
    }
}
