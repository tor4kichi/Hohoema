using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mntone.Nico2.Channels.Video;
using Hohoema.Models.Helpers;
using Hohoema.Models.Domain;
using Prism.Commands;
using Prism.Navigation;
using Hohoema.Models.UseCase.NicoVideos;
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

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Video
{
    using ChannelFollowContext = FollowContext<ChannelFollowProvider>;


    public sealed class ChannelInfo : IChannel
    {
        public string Id { get; set; }

        public string Label { get; set; }

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

        public string RawChannelId { get; set; }

        private int? _ChannelId;
        public int? ChannelId
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
            if (parameters.TryGetValue("id", out string id))
            {
                RawChannelId = id;

                ChannelInfo = null;

                try
                {
                    var channelInfo = await ChannelProvider.GetChannelInfo(RawChannelId);

                    ChannelId = channelInfo.ChannelId;
                    ChannelName = channelInfo.Name;
                    ChannelScreenName = channelInfo.ScreenName;
                    ChannelOpenTime = channelInfo.ParseOpenTime();
                    ChannelUpdateTime = channelInfo.ParseUpdateTime();
                    ChannelInfo = new ChannelInfo() { Id = ChannelId?.ToString(), Label = ChannelName };
                }
                catch
                {
                    ChannelName = RawChannelId;
                }

                try
                {
                    if (NiconicoSession.IsLoggedIn)
                    {
                        FollowContext = await ChannelFollowContext.CreateAsync(_channelFollowProvider, ChannelId.ToString());
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

            await base.OnNavigatedToAsync(parameters);
        }


        protected override IIncrementalSource<ChannelVideoListItemViewModel> GenerateIncrementalSource()
        {
            return new ChannelVideoLoadingSource(ChannelId?.ToString() ?? RawChannelId, ChannelProvider);
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

        public ChannelVideoListItemViewModel(string rawVideoId, string title, string thumbnailUrl, TimeSpan videoLength) 
            : base(rawVideoId, title, thumbnailUrl, videoLength)
        {
        }
    }

    public class ChannelVideoLoadingSource : HohoemaIncrementalSourceBase<ChannelVideoListItemViewModel>
    {
        public string ChannelId { get; }
        public ChannelProvider ChannelProvider { get; }

        ChannelVideoResponse _FirstResponse;
        public ChannelVideoLoadingSource(string channelId, ChannelProvider channelProvider)
        {
            ChannelId = channelId;
            ChannelProvider = channelProvider;
        }


        public override uint OneTimeLoadCount => 20;

        bool _IsEndPage = false;

        protected override async IAsyncEnumerable<ChannelVideoListItemViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (_FirstResponse == null)
            {
                yield break;
            }

            if (_IsEndPage)
            {
                yield break;
            }

            ChannelVideoResponse res;

            if (head == 0)
            {
                res = _FirstResponse;
            }
            else
            {
                var page = (int)(head / OneTimeLoadCount);
                res = await ChannelProvider.GetChannelVideo(ChannelId, page);
            }

            ct.ThrowIfCancellationRequested();

            _IsEndPage = res != null ? (res.Videos.Count < OneTimeLoadCount) : true;

            if (res != null)
            {
                foreach (var video in res.Videos)
                {
                    // so0123456のフォーマットの動画ID
                    // var videoId = video.PurchasePreviewUrl.Split('/').Last();

                    var channelVideo = new ChannelVideoListItemViewModel(video.ItemId, video.Title, video.ThumbnailUrl, video.Length);
                    if (video.IsRequirePayment)
                    {
                        channelVideo.Permission = NiconicoLiveToolkit.Video.VideoPermission.RequirePay;
                    }
                    else if (video.IsFreeForMember)
                    {
                        channelVideo.Permission = NiconicoLiveToolkit.Video.VideoPermission.FreeForChannelMember;
                    }
                    else if (video.IsMemberUnlimitedAccess)
                    {
                        channelVideo.Permission = NiconicoLiveToolkit.Video.VideoPermission.MemberUnlimitedAccess;
                    }
                    channelVideo.PostedAt = video.PostedAt;
                    channelVideo.ViewCount = video.ViewCount;
                    channelVideo.CommentCount = video.CommentCount;
                    channelVideo.MylistCount = video.MylistCount; 

                    yield return channelVideo;

                    ct.ThrowIfCancellationRequested();
                }
            }
            else
            {
                yield break;
            }
        }

        protected override async ValueTask<int> ResetSourceImpl()
        {
            _FirstResponse = await ChannelProvider.GetChannelVideo(ChannelId, 0);

            return _FirstResponse?.TotalCount ?? 0;
        }
    }

}
