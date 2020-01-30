using Windows.UI.Xaml.Controls;
using Unity;
using Windows.UI.Xaml;
using Prism.Ioc;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Commands.Mylist;
using NicoPlayerHohoema.Commands.Subscriptions;
using NicoPlayerHohoema.Commands;
using NicoPlayerHohoema.Commands.Cache;
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


        public IReadOnlyCollection<Interfaces.IVideoContent> VideoItems
        {
            get { return (IReadOnlyCollection<Interfaces.IVideoContent>)GetValue(VideoItemsProperty); }
            set { SetValue(VideoItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VideoItemsProperty =
            DependencyProperty.Register(nameof(VideoItems), typeof(IReadOnlyCollection<Interfaces.IVideoContent>), typeof(VideoItemFlyout), new PropertyMetadata(null));




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
        public Commands.Subscriptions.CreateSubscriptionGroupCommand CreateSubscriptionGroupCommand { get; }



        readonly private string _localizedText_CreateNew = "CreateNew".Translate();

        public VideoItemFlyout()
        {
            this.InitializeComponent();

            VideoItems = new List<IVideoContent>();

            CreateMylistCommand = App.Current.Container.Resolve<CreateMylistCommand>();
            CreateLocalMylistCommand = App.Current.Container.Resolve<CreateLocalMylistCommand>();
            CreateSubscriptionGroupCommand = App.Current.Container.Resolve<CreateSubscriptionGroupCommand>();
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
            AddQueue.Command = App.Current.Container.Resolve<QueueAddItemCommand>();
            RemoveQueue.Command = App.Current.Container.Resolve<QueueRemoveItemCommand>();

            OpenVideoInfoPage.Command = PageManager.OpenPageCommand;
            OpenOwnerVideosPage.Command = PageManager.OpenVideoListPageCommand;
            OpenOwnerSeriesPage.Command = new OpenPageWithIdCommand(HohoemaPageType.UserSeries, PageManager);
            Share.Command = ExternalAccessService.OpenShareUICommand;
            CopyVideoId.Command = ExternalAccessService.CopyToClipboardCommand;
            CopyVideoId.Command = ExternalAccessService.CopyToClipboardCommand;
            CopyShareText.Command = ExternalAccessService.CopyToClipboardWithShareTextCommand;

            CacheRequest.Command = App.Current.Container.Resolve<Commands.Cache.AddCacheRequestCommand>();
            DeleteCacheRequest.Command = App.Current.Container.Resolve<DeleteCacheRequestCommand>();

            AddNgUser.Command = App.Current.Container.Resolve<AddToHiddenUserCommand>();

            SelectionStart.Command = App.Current.Container.Resolve<SelectionStartCommand>();
            SelectionEnd.Command = App.Current.Container.Resolve<SelectionExitCommand>();
            SelectionAll.Command = App.Current.Container.Resolve<SelectionAllSelectCommand>();

            Opening += VideoItemFlyout_Opening;
        }

        private void VideoItemFlyout_Opening(object sender, object e)
        {
            object dataContext = Target.DataContext ?? (Target as SelectorItem)?.Content;
            var content = (dataContext as Interfaces.IVideoContent);

            if (content == null || (VideoItems?.Any() ?? false))
            {
                content = VideoItems?.First();
                dataContext = VideoItems;
            }

            bool isMultipleSelection = VideoItems?.Count >= 2;

            var playlist = Playlist;


            // 視聴履歴
            RemoveWatchHisotryItem.Visibility = (content is IWatchHistory).ToVisibility();

            // ローカルプレイリスト
            if (playlist is LocalPlaylist localPlaylist)
            {
                RemoveLocalPlaylistItem.CommandParameter = dataContext;
                RemoveLocalPlaylistItem.Command = new LocalPlaylistRemoveItemCommand(localPlaylist, LocalMylistManager);
                RemoveLocalPlaylistItem.Visibility = Visibility.Visible;
            }
            else
            {
                RemoveLocalPlaylistItem.Visibility = Visibility.Collapsed;
            }

            // マイリスト
            if (playlist is MylistPlaylist mylistPlaylist)
            {
                RemoveMylistItem.CommandParameter = dataContext;
                RemoveMylistItem.Command = new MylistRemoveItemCommand(mylistPlaylist, UserMylistManager);
                RemoveMylistItem.Visibility = Visibility.Visible;
            }
            else
            {
                RemoveMylistItem.Visibility = Visibility.Collapsed;
            }

            // キュー
            AddQueue.CommandParameter = dataContext;
            RemoveQueue.CommandParameter = dataContext;
            if (isMultipleSelection)
            {
                AddQueue.Visibility = VideoItems.All(x => HohoemaPlaylist.QueuePlaylist.Contains(x)).ToInvisibility();
                RemoveQueue.Visibility = VideoItems.Any(x => HohoemaPlaylist.QueuePlaylist.Contains(x)).ToVisibility();
            }
            else if (HohoemaPlaylist.QueuePlaylist.Contains(content))
            {
                AddQueue.Visibility = Visibility.Collapsed;
                RemoveQueue.Visibility = Visibility.Visible;
            }
            else
            {
                AddQueue.Visibility = Visibility.Visible;
                RemoveQueue.Visibility = Visibility.Collapsed;
            }

            // あとで見る
            AddWatchAfter.CommandParameter = dataContext;
            RemoveWatchAfter.CommandParameter = dataContext;
            if (isMultipleSelection)
            {
                AddWatchAfter.Visibility = VideoItems.All(x => HohoemaPlaylist.WatchAfterPlaylist.Contains(x)).ToInvisibility();
                RemoveWatchAfter.Visibility = VideoItems.Any(x => HohoemaPlaylist.WatchAfterPlaylist.Contains(x)).ToVisibility();
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
                        Command = new UseCase.Playlist.Commands.MylistAddItemCommand(mylist, UserMylistManager),
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

            SusbcriptionSubItem.Visibility = visibleSingleSelectionItem;


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
                    Command = new UseCase.Playlist.Commands.LocalPlaylistAddItemCommand(localMylist, LocalMylistManager),
                    CommandParameter = dataContext
                });
            }

            // 購読
            var susbcSourceConverter = new Subscriptions.SubscriptionSourceConverter();
            var subscSource = susbcSourceConverter.Convert(content, typeof(SubscriptionSource), null, CultureInfo.CurrentCulture.Name);
            SusbcriptionSubItem.Items.Clear();
            SusbcriptionSubItem.Items.Clear();
            SusbcriptionSubItem.Items.Add(new MenuFlyoutItem()
            {
                Text = _localizedText_CreateNew,
                Command = CreateSubscriptionGroupCommand,
                CommandParameter = subscSource
            });

            foreach (var subsc in SubscriptionManager.Subscriptions)
            {
                SusbcriptionSubItem.Items.Add(new MenuFlyoutItem()
                {
                    Text = subsc.Label,
                    Command = subsc.AddSource,
                    CommandParameter = subscSource
                });
            }

            // キャッシュ
            var isCacheEnabled = VideoCacheManager.CacheSettings.IsEnableCache && VideoCacheManager.CacheSettings.IsUserAcceptedCache;
            var visibleCacheItem = isCacheEnabled.ToVisibility();
            CacheRequest.Visibility = visibleCacheItem;
            CacheRequest.CommandParameter = dataContext;
            DeleteCacheRequest.Visibility = visibleCacheItem;
            DeleteCacheRequest.CommandParameter = dataContext;
            CacheSeparator.Visibility = visibleCacheItem;

            // 選択
            if (!VideoItemsSelectionContext.IsSelectionEnabled)
            {
                SelectionStart.Visibility = Visibility.Visible;
                SelectionEnd.Visibility = Visibility.Collapsed;
                SelectionAll.Visibility = Visibility.Collapsed;
            }
            else
            {
                SelectionStart.Visibility = Visibility.Collapsed;
                SelectionEnd.Visibility = Visibility.Visible;
                SelectionAll.Visibility = Visibility.Visible;
            }
        }
    }
}
