using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hohoema.Helpers;
using Hohoema.Models;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Services.Playlist;
using Reactive.Bindings.Extensions;
using Hohoema.Services;
using System.Runtime.CompilerServices;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Services.PageNavigation;
using Hohoema.Services;
using Hohoema.Models.Niconico.Video;
using Hohoema.ViewModels.VideoListPage;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.Models.Pins;
using Hohoema.Models.Niconico;
using Hohoema.ViewModels.Niconico.Follow;
using Hohoema.ViewModels.Niconico.Share;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Channels;
using NiconicoToolkit;
using Hohoema.Models.Playlist;
using Reactive.Bindings;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Hohoema.Navigations;

namespace Hohoema.ViewModels.Pages.Niconico.Channel
{
    using ChannelFollowContext = FollowContext<IChannel>;


    public sealed class ChannelInfo : IChannel
    {
        public ChannelId ChannelId { get; set; }

        public string Name { get; set; }

    }

    public sealed class ChannelVideoPageViewModel : HohoemaListingPageViewModelBase<ChannelVideoListItemViewModel>, IPinablePage, ITitleUpdatablePage
    {
        HohoemaPin IPinablePage.GetPin()
        {
            return new HohoemaPin()
            {
                Label = ChannelName,
                PageType = HohoemaPageType.ChannelVideo,
                Parameter = $"id={ChannelId}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.ChannelName);
        }

        public ChannelVideoPageViewModel(
            ILoggerFactory loggerFactory,
            ApplicationLayoutManager applicationLayoutManager,
            NiconicoSession niconicoSession,
            ChannelProvider channelProvider,
            ChannelFollowProvider channelFollowProvider,
            PageManager pageManager,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand,
            OpenLinkCommand openLinkCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
            : base(loggerFactory.CreateLogger<ChannelVideoPageViewModel>())
        {
            ApplicationLayoutManager = applicationLayoutManager;
            NiconicoSession = niconicoSession;
            ChannelProvider = channelProvider;
            _channelFollowProvider = channelFollowProvider;
            PageManager = pageManager;
            VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
            OpenLinkCommand = openLinkCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;

            CurrentPlaylistToken = Observable.CombineLatest(
                this.ObserveProperty(x => x.ChannelVideoPlaylist),
                this.ObserveProperty(x => x.SelectedSortOption),
                (x, y) => new PlaylistToken(x, y)
                )
                .ToReadOnlyReactivePropertySlim()
                .AddTo(_CompositeDisposable);
        }

        private ChannelId? _ChannelId;
        public ChannelId? ChannelId
        {
            get { return _ChannelId; }
            set { SetProperty(ref _ChannelId, value); }
        }

        private string _ChannelName;
        public string ChannelName
        {
            get { return _ChannelName; }
            set { SetProperty(ref _ChannelName, value); }
        }

        private string _ChannelScreenName;
        public string ChannelScreenName
        {
            get { return _ChannelScreenName; }
            set { SetProperty(ref _ChannelScreenName, value); }
        }

        private string _ChannelCompanyName;
        public string ChannelCompanyName
        {
            get { return _ChannelCompanyName; }
            set { SetProperty(ref _ChannelCompanyName, value); }
        }

        private DateTime _ChannelOpenTime;
        public DateTime ChannelOpenTime
        {
            get { return _ChannelOpenTime; }
            set { SetProperty(ref _ChannelOpenTime, value); }
        }

        private DateTime _ChannelUpdateTime;
        public DateTime ChannelUpdateTime
        {
            get { return _ChannelUpdateTime; }
            set { SetProperty(ref _ChannelUpdateTime, value); }
        }


        private ChannelInfo _channelInfo;
        public ChannelInfo ChannelInfo
        {
            get { return _channelInfo; }
            set { SetProperty(ref _channelInfo, value); }
        }

        // Follow
        private ChannelFollowContext _FollowContext = ChannelFollowContext.Default;
        public ChannelFollowContext FollowContext
        {
            get => _FollowContext;
            set => SetProperty(ref _FollowContext, value);
        }


        private ChannelVideoPlaylist _ChannelVideoPlaylist;
        public ChannelVideoPlaylist ChannelVideoPlaylist
        {
            get { return _ChannelVideoPlaylist; }
            set { SetProperty(ref _ChannelVideoPlaylist, value); }
        }

        public ChannelVideoPlaylistSortOption[] SortOptions => ChannelVideoPlaylist.SortOptions;


        private ChannelVideoPlaylistSortOption _selectedSortOption = ChannelVideoPlaylist.DefaultSortOption;
        public ChannelVideoPlaylistSortOption SelectedSortOption
        {
            get { return _selectedSortOption; }
            set { SetProperty(ref _selectedSortOption, value); }
        }

        public ReadOnlyReactivePropertySlim<PlaylistToken> CurrentPlaylistToken { get; }


        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            ChannelId = null;
            ChannelInfo = null;

            if (parameters.TryGetValue("id", out string id))
            {
                ChannelId = id;
            }
            else if (parameters.TryGetValue("id", out uint nonPrefixId))
            {
                ChannelId = nonPrefixId;
            }
            else if (parameters.TryGetValue("id", out ChannelId channelId))
            {
                ChannelId = channelId;
            }

            if (ChannelId != null)
            {
                await UpdateChannelInfo();
            }

            await base.OnNavigatedToAsync(parameters);
        }


        async Task UpdateChannelInfo()
        {
            try
            {
                var channelInfo = await ChannelProvider.GetChannelInfo(ChannelId.Value);

                ChannelId = channelInfo.ChannelId;
                ChannelName = channelInfo.Name;
                ChannelScreenName = channelInfo.ScreenName;
                ChannelOpenTime = channelInfo.ParseOpenTime();
                ChannelUpdateTime = channelInfo.ParseUpdateTime();
                ChannelInfo = new ChannelInfo() { ChannelId = channelInfo.ChannelId, Name = ChannelName };

                await UpdateFollowChannelAsync(ChannelInfo);

                ChannelVideoPlaylist = new ChannelVideoPlaylist(channelInfo.ChannelId, new PlaylistId() { Id = channelInfo.ChannelId, Origin = PlaylistItemsSourceOrigin.ChannelVideos }, channelInfo.Name, ChannelProvider);
                SelectedSortOption = ChannelVideoPlaylist.DefaultSortOption;

                this.ObserveProperty(x => x.SelectedSortOption)
                    .Subscribe(_ => ResetList())
                    .AddTo(_navigationDisposables);
            }
            catch
            {
                ChannelName = ChannelId;
            }
        }

        async Task UpdateFollowChannelAsync(ChannelInfo channelInfo)
        {
            try
            {
                if (NiconicoSession.IsLoggedIn)
                {
                    FollowContext = await ChannelFollowContext.CreateAsync(_channelFollowProvider, channelInfo);
                }
                else
                {
                    FollowContext = ChannelFollowContext.Default;
                }
            }
            catch
            {
                FollowContext = ChannelFollowContext.Default;
            }
        }

        protected override (int, IIncrementalSource<ChannelVideoListItemViewModel>) GenerateIncrementalSource()
        {
            return (ChannelVideoLoadingSource.OneTimeLoadCount, new ChannelVideoLoadingSource(ChannelId.Value, ChannelProvider, ChannelVideoPlaylist, SelectedSortOption));
        }


        private RelayCommand _ShowWithBrowserCommand;
        private readonly ChannelFollowProvider _channelFollowProvider;

        public RelayCommand ShowWithBrowserCommand
        {
            get
            {
                return _ShowWithBrowserCommand ??
                    (_ShowWithBrowserCommand = new RelayCommand(async () => 
                    {
                        await Windows.System.Launcher.LaunchUriAsync(new Uri($"http://ch.nicovideo.jp/{ChannelScreenName}/video"));
                    }));
            }
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public NiconicoSession NiconicoSession { get; }
        public ChannelProvider ChannelProvider { get; }
        public PageManager PageManager { get; }
        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
        public OpenLinkCommand OpenLinkCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
    }

    public sealed class ChannelVideoListItemViewModel : VideoListItemControlViewModel
    {
        public ChannelVideoListItemViewModel(
           NicoVideo data
           )
           : base(data)
        {

        }

        public ChannelVideoListItemViewModel(string rawVideoId, string title, string thumbnailUrl, TimeSpan videoLength, DateTime postedAt) 
            : base(rawVideoId, title, thumbnailUrl, videoLength, postedAt)
        {
        }
    }

    public class ChannelVideoLoadingSource : IIncrementalSource<ChannelVideoListItemViewModel>
    {
        public ChannelId ChannelId { get; }
        public ChannelProvider ChannelProvider { get; }

        public ChannelVideoLoadingSource(ChannelId channelId,  ChannelProvider channelProvider, ChannelVideoPlaylist channelVideoPlaylist, ChannelVideoPlaylistSortOption sortOption)
        {
            ChannelId = channelId;
            ChannelProvider = channelProvider;
            _channelVideoPlaylist = channelVideoPlaylist;
            _sortOption = sortOption;
        }


        public const int OneTimeLoadCount = 20;
        private readonly ChannelVideoPlaylist _channelVideoPlaylist;
        private readonly ChannelVideoPlaylistSortOption _sortOption;
        bool _IsEndPage = false;

        async Task<IEnumerable<ChannelVideoListItemViewModel>> IIncrementalSource<ChannelVideoListItemViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
        {
            if (_IsEndPage) { return Enumerable.Empty<ChannelVideoListItemViewModel>(); }

            var res = await ChannelProvider.GetChannelVideo(ChannelId, pageIndex, _sortOption.SortKey, _sortOption.SortOrder);

            ct.ThrowIfCancellationRequested();

            _IsEndPage = res != null ? (res.Data.Videos.Length < OneTimeLoadCount) : true;

            if (!res.IsSuccess) { return Enumerable.Empty<ChannelVideoListItemViewModel>(); }

            return ToChannelVideoVMItems(res.Data.Videos, head: pageIndex * pageSize)
                .ToArray()// Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
                ;
        }

        IEnumerable<ChannelVideoListItemViewModel> ToChannelVideoVMItems(ChannelVideoItem[] items, int head)
        {
            int i = 0;
            foreach (var video in items)
            {
                // so0123456のフォーマットの動画ID
                // var videoId = video.PurchasePreviewUrl.Split('/').Last();

                var channelVideo = new ChannelVideoListItemViewModel(video.ItemId, video.Title, video.ThumbnailUrl, video.Length, video.PostedAt)
                {
                    PlaylistItemToken = new PlaylistItemToken(_channelVideoPlaylist, _sortOption, new ChannelVideoContent(video, ChannelId))
                };

                if (video.IsRequirePayment)
                {
                    channelVideo.Permission = NiconicoToolkit.Video.VideoPermission.RequirePay;
                }
                else if (video.IsFreeForMember)
                {
                    channelVideo.Permission = NiconicoToolkit.Video.VideoPermission.FreeForChannelMember;
                }
                else if (video.IsMemberUnlimitedAccess)
                {
                    channelVideo.Permission = NiconicoToolkit.Video.VideoPermission.MemberUnlimitedAccess;
                }
                channelVideo.ViewCount = video.ViewCount;
                channelVideo.CommentCount = video.CommentCount;
                channelVideo.MylistCount = video.MylistCount;

                yield return channelVideo;
            }
        }
    }

}
