using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hohoema.Models.Helpers;
using Hohoema.Models;
using Prism.Commands;
using Hohoema.Interfaces;
using Prism.Navigation;
using Hohoema.Services;
using Hohoema.UseCase.Playlist;
using Reactive.Bindings.Extensions;

using Hohoema.UseCase;
using Hohoema.Models.Pages;
using Hohoema.Models.Niconico;
using Hohoema.Models.Repository.Niconico.Channel;
using Hohoema.Models.Repository.Niconico;
using Hohoema.Models.Repository;
using System.Runtime.CompilerServices;
using Hohoema.ViewModels.Pages;
using Hohoema.ViewModels.Player.Commands;
using Hohoema.ViewModels.ExternalAccess.Commands;

namespace Hohoema.ViewModels
{
    public sealed class ChannelVideoPageViewModel 
        : HohoemaListingPageViewModelBase<ChannelVideoListItemViewModel>,
        IChannel,
        INavigatedAwareAsync, 
        IPinablePage, 
        ITitleUpdatablePage
    {
        Models.Pages.HohoemaPin IPinablePage.GetPin()
        {
            return new Models.Pages.HohoemaPin()
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
            NiconicoFollowToggleButtonService followToggleButtonService,
            PlayVideoCommand playVideoCommand,
            OpenLinkCommand openLinkCommand
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            NiconicoSession = niconicoSession;
            ChannelProvider = channelProvider;
            PageManager = pageManager;
            HohoemaPlaylist = hohoemaPlaylist;
            FollowToggleButtonService = followToggleButtonService;
            PlayVideoCommand = playVideoCommand;
            OpenLinkCommand = openLinkCommand;
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
                    ChannelOpenTime = channelInfo.OpenTime;
                    ChannelUpdateTime = channelInfo.UpdateTime;
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
        public ChannelProvider ChannelProvider { get; }
        public PageManager PageManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public NiconicoFollowToggleButtonService FollowToggleButtonService { get; }
        public PlayVideoCommand PlayVideoCommand { get; }
        public OpenLinkCommand OpenLinkCommand { get; }

        string INiconicoObject.Id => RawChannelId;

        string INiconicoObject.Label => ChannelName;
    }

    public sealed class ChannelVideoListItemViewModel : VideoInfoControlViewModel
    {
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

        protected override async IAsyncEnumerable<ChannelVideoListItemViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation]CancellationToken cancellationToken)
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

            _IsEndPage = res != null ? (res.Videos.Count < OneTimeLoadCount) : true;

            if (res != null)
            {
                foreach (var item in res.Videos.Select(video =>
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
                }))
                {
                    yield return item;
                }
            }
            else
            {
                yield break;
            }
        }

        protected override async Task<int> ResetSourceImpl()
        {
            _FirstResponse = await ChannelProvider.GetChannelVideo(ChannelId, 0);

            return _FirstResponse?.TotalCount ?? 0;
        }
    }

}
