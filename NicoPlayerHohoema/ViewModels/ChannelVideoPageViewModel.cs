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
using Prism.Windows.Navigation;
using Windows.UI;
using NicoPlayerHohoema.Models.Provider;
using Microsoft.Practices.Unity;
using NicoPlayerHohoema.Interfaces;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class ChannelVideoPageViewModel : HohoemaVideoListingPageViewModelBase<ChannelVideoListItemViewModel>, Interfaces.IChannel
    {
        public ChannelVideoPageViewModel(
            Models.Provider.ChannelProvider channelProvider,
            Services.PageManager pageManager,
            Services.HohoemaPlaylist hohoemaPlaylist,
            Services.ExternalAccessService externalAccessService
            )
            : base(pageManager, useDefaultPageTitle:true)
        {
            ChannelProvider = channelProvider;
            HohoemaPlaylist = hohoemaPlaylist;
            ExternalAccessService = externalAccessService;
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


        protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (e.Parameter is string)
            {
                RawChannelId = e.Parameter as string;
            }

            if (RawChannelId == null) { return; }

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

            PageManager.PageTitle = ChannelName;

            await base.NavigatedToAsync(cancelToken, e, viewModelState);
        }

        protected override IIncrementalSource<ChannelVideoListItemViewModel> GenerateIncrementalSource()
        {
            return new ChannelVideoLoadingSource(ChannelId?.ToString() ?? RawChannelId, ChannelProvider);
        }

        protected override string ResolvePageName()
        {
            return ChannelName ?? base.ResolvePageName();
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

        public Models.Provider.ChannelProvider ChannelProvider { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public Services.ExternalAccessService ExternalAccessService { get; }

        string INiconicoObject.Id => RawChannelId;

        string INiconicoObject.Label => ChannelName;
    }

    public sealed class ChannelVideoListItemViewModel : VideoInfoControlViewModel
    {
        public bool IsRequirePayment { get; internal set; }

        public ChannelVideoListItemViewModel()
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

                    var channelVideo = App.Current.Container.Resolve<ChannelVideoListItemViewModel>();
                    channelVideo.RawVideoId = video.ItemId;
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
