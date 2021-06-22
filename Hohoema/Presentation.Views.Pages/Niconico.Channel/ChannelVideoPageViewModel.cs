using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hohoema.Models.Helpers;
using Hohoema.Models.Domain;
using Prism.Commands;
using Prism.Navigation;
using Hohoema.Models.UseCase.Playlist;
using Reactive.Bindings.Extensions;
using Hohoema.Models.UseCase;
using System.Runtime.CompilerServices;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.Services;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Presentation.ViewModels.Niconico.Follow;
using Hohoema.Presentation.ViewModels.Niconico.Share;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Channels;
using NiconicoToolkit;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Channel
{
    using ChannelFollowContext = FollowContext<IChannel>;


    public sealed class ChannelInfo : IChannel
    {
        public ChannelId ChannelId { get; set; }

        public string Name { get; set; }

    }

    public sealed class ChannelVideoPageViewModel : HohoemaListingPageViewModelBase<ChannelVideoListItemViewModel>, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
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
            ApplicationLayoutManager applicationLayoutManager,
            NiconicoSession niconicoSession,
            ChannelProvider channelProvider,
            ChannelFollowProvider channelFollowProvider,
            PageManager pageManager,
            HohoemaPlaylist hohoemaPlaylist,
            OpenLinkCommand openLinkCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            NiconicoSession = niconicoSession;
            ChannelProvider = channelProvider;
            _channelFollowProvider = channelFollowProvider;
            PageManager = pageManager;
            HohoemaPlaylist = hohoemaPlaylist;
            OpenLinkCommand = openLinkCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
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
            return (ChannelVideoLoadingSource.OneTimeLoadCount, new ChannelVideoLoadingSource(ChannelId.Value, ChannelProvider));
        }


        private DelegateCommand _ShowWithBrowserCommand;
        private readonly ChannelFollowProvider _channelFollowProvider;

        public DelegateCommand ShowWithBrowserCommand
        {
            get
            {
                return _ShowWithBrowserCommand ??
                    (_ShowWithBrowserCommand = new DelegateCommand(async () => 
                    {
                        await Windows.System.Launcher.LaunchUriAsync(new Uri($"http://ch.nicovideo.jp/{ChannelScreenName}/video"));
                    }));
            }
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public NiconicoSession NiconicoSession { get; }
        public ChannelProvider ChannelProvider { get; }
        public PageManager PageManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
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

        public ChannelVideoLoadingSource(ChannelId channelId, ChannelProvider channelProvider)
        {
            ChannelId = channelId;
            ChannelProvider = channelProvider;
        }


        public const int OneTimeLoadCount = 20;

        bool _IsEndPage = false;

        async Task<IEnumerable<ChannelVideoListItemViewModel>> IIncrementalSource<ChannelVideoListItemViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
        {
            if (_IsEndPage) { return Enumerable.Empty<ChannelVideoListItemViewModel>(); }

            var res = await ChannelProvider.GetChannelVideo(ChannelId, pageIndex);

            ct.ThrowIfCancellationRequested();

            _IsEndPage = res != null ? (res.Data.Videos.Length < OneTimeLoadCount) : true;

            if (!res.IsSuccess) { return Enumerable.Empty<ChannelVideoListItemViewModel>(); }

            return ToChannelVideoVMItems(res.Data.Videos);
        }

        IEnumerable<ChannelVideoListItemViewModel> ToChannelVideoVMItems(ChannelVideoItem[] items)
        {
            foreach (var video in items)
            {
                // so0123456のフォーマットの動画ID
                // var videoId = video.PurchasePreviewUrl.Split('/').Last();

                var channelVideo = new ChannelVideoListItemViewModel(video.ItemId, video.Title, video.ThumbnailUrl, video.Length, video.PostedAt);
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
