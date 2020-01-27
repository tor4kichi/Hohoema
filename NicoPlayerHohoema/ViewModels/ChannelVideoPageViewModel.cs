using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mntone.Nico2.Channels.Video;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Windows.UI;
using NicoPlayerHohoema.Models.Provider;
using Unity;
using NicoPlayerHohoema.Interfaces;
using Prism.Navigation;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.UseCase.Playlist;
using Reactive.Bindings.Extensions;
using NicoPlayerHohoema.UseCase;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class ChannelVideoPageViewModel : HohoemaListingPageViewModelBase<ChannelVideoListItemViewModel>, Interfaces.IChannel, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
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
            PageManager pageManager,
            HohoemaPlaylist hohoemaPlaylist,
            ExternalAccessService externalAccessService,
            NiconicoFollowToggleButtonService followToggleButtonService
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            NiconicoSession = niconicoSession;
            ChannelProvider = channelProvider;
            PageManager = pageManager;
            HohoemaPlaylist = hohoemaPlaylist;
            ExternalAccessService = externalAccessService;
            FollowToggleButtonService = followToggleButtonService;
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


        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            if (parameters.TryGetValue("id", out string id))
            {
                RawChannelId = id;

                try
                {
                    var channelInfo = await ChannelProvider.GetChannelInfo(RawChannelId);

                    ChannelId = channelInfo.ChannelId;
                    ChannelName = channelInfo.Name;
                    ChannelScreenName = channelInfo.ScreenName;
                    ChannelOpenTime = channelInfo.ParseOpenTime();
                    ChannelUpdateTime = channelInfo.ParseUpdateTime();
                }
                catch
                {
                    ChannelName = RawChannelId;
                }

                FollowToggleButtonService.SetFollowTarget(this);
            }

            await base.OnNavigatedToAsync(parameters);
        }


        protected override IIncrementalSource<ChannelVideoListItemViewModel> GenerateIncrementalSource()
        {
            return new ChannelVideoLoadingSource(ChannelId?.ToString() ?? RawChannelId, ChannelProvider);
        }


        private DelegateCommand _ShowWithBrowserCommand;
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
        public Models.Provider.ChannelProvider ChannelProvider { get; }
        public Services.PageManager PageManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public Services.ExternalAccessService ExternalAccessService { get; }
        public Services.NiconicoFollowToggleButtonService FollowToggleButtonService { get; }
        
        string INiconicoObject.Id => RawChannelId;

        string INiconicoObject.Label => ChannelName;
    }

    public sealed class ChannelVideoListItemViewModel : VideoInfoControlViewModel
    {
        public bool IsRequirePayment { get; internal set; }

        public ChannelVideoListItemViewModel(
            string rawVideoId
            )
            : base(rawVideoId)
        {

        }

        public ChannelVideoListItemViewModel(
           Database.NicoVideo data
           )
           : base(data)
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

        protected override async Task<IAsyncEnumerable<ChannelVideoListItemViewModel>> GetPagedItemsImpl(int head, int count)
        {
            if (_FirstResponse == null)
            {
                return AsyncEnumerable.Empty<ChannelVideoListItemViewModel>();
            }

            if (_IsEndPage)
            {
                return AsyncEnumerable.Empty<ChannelVideoListItemViewModel>();
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

            _IsEndPage = res != null ? (res.Videos.Count < OneTimeLoadCount) : true;

            if (res != null)
            {
                return res.Videos.Select(video => 
                {
                    // so0123456のフォーマットの動画ID
                    // var videoId = video.PurchasePreviewUrl.Split('/').Last();

                    var channelVideo = new ChannelVideoListItemViewModel(video.ItemId);
                    channelVideo.IsRequirePayment = video.IsRequirePayment;
                    channelVideo.SetTitle(video.Title);
                    channelVideo.SetThumbnailImage(video.ThumbnailUrl);
                    channelVideo.SetVideoDuration(video.Length);
                    channelVideo.SetSubmitDate(video.PostedAt);
                    channelVideo.SetDescription(video.ViewCount, video.CommentCount, video.MylistCount);                    

                    return channelVideo;
                }
                ).ToAsyncEnumerable();
            }
            else
            {
                return AsyncEnumerable.Empty<ChannelVideoListItemViewModel>();
            }
        }

        protected override async Task<int> ResetSourceImpl()
        {
            _FirstResponse = await ChannelProvider.GetChannelVideo(ChannelId, 0);

            return _FirstResponse?.TotalCount ?? 0;
        }
    }

}
