using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Microsoft.Toolkit.Uwp.UI;
using Mntone.Nico2.Channels.Video;
using NiconicoToolkit;
using NiconicoToolkit.Recommend;
using NiconicoToolkit.Video;
using Prism.Commands;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Player.PlayerSidePaneContent
{
    public sealed class RelatedVideosSidePaneContentViewModel : SidePaneContentViewModelBase
    {
        public RelatedVideosSidePaneContentViewModel(
            NiconicoSession niconicoSession,
            HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager
           )
        {
            _niconicoSession = niconicoSession;
            _hohoemaPlaylist = hohoemaPlaylist;
            _pageManager = pageManager;
            PlayCommand = _hohoemaPlaylist.PlayCommand;
            OpenMylistCommand = _pageManager.OpenPageCommand;
        }


        public DelegateCommand<object> PlayCommand { get; }
        public DelegateCommand<object> OpenMylistCommand { get; }

        public List<VideoListItemControlViewModel> Videos { get; private set; }

        public VideoListItemControlViewModel CurrentVideo { get; private set; }
        public VideoListItemControlViewModel NextVideo { get; private set; }

        public string JumpVideoId { get; set; }
        public bool HasVideoDescription { get; private set; }

        public Models.Helpers.AsyncLock _InitializeLock = new Models.Helpers.AsyncLock();

        private bool _IsInitialized = false;
        private readonly NiconicoSession _niconicoSession;
        private readonly HohoemaPlaylist _hohoemaPlaylist;
        private readonly PageManager _pageManager;


        public bool NowLoading { get; private set; }

        public void Clear()
        {
            CurrentVideo?.Dispose();
            CurrentVideo = null;
            RaisePropertyChanged(nameof(CurrentVideo));

            NextVideo?.Dispose();
            NextVideo = null;
            RaisePropertyChanged(nameof(NextVideo));


            if (Videos != null)
            {
                foreach (var item in Videos)
                {
                    item.Dispose();
                }
                Videos.Clear();
                RaisePropertyChanged(nameof(Videos));
            }

            _IsInitialized = false;
        }

        public async Task InitializeRelatedVideos(INicoVideoDetails currentVideo)
        {
            string videoId = currentVideo.VideoId;

            NowLoading = true;
            RaisePropertyChanged(nameof(NowLoading));
            try
            {
                using (var releaser = await _InitializeLock.LockAsync())
                {
                    if (_IsInitialized) { return; }
                    _IsInitialized = true;

                    CurrentVideo = new VideoListItemControlViewModel(currentVideo);
                    RaisePropertyChanged(nameof(CurrentVideo));

                    VideoRecommendResponse recommendResponse = null;
                    if (currentVideo is IVideoContentProvider provider)
                    {
                        if (provider.ProviderType == OwnerType.Channel)
                        {
                            recommendResponse = await _niconicoSession.ToolkitContext.Recommend.GetChannelVideoReccommendAsync(currentVideo.Id, provider.ProviderId, currentVideo.Tags.Select(x => x.Tag).ToArray());
                        }
                    }

                    if (recommendResponse == null)
                    {
                        recommendResponse = await _niconicoSession.ToolkitContext.Recommend.GetVideoReccommendAsync(currentVideo.Id);
                    }

                    if (recommendResponse?.IsSuccess ?? false)
                    {
                        Videos = new List<VideoListItemControlViewModel>();
                        foreach (var item in recommendResponse.Data.Items)
                        {
                            if (item.ContentType is RecommendContentType.Video)
                            {
                                Videos.Add(new VideoListItemControlViewModel(item.ContentAsVideo));
                            }
                        }
                        RaisePropertyChanged(nameof(Videos));
                    }

                    if (currentVideo.Series?.Video.Next is not null and NvapiVideoItem nextSeriesVideo)
                    {
                        NextVideo = new VideoListItemControlViewModel(nextSeriesVideo);
                        RaisePropertyChanged(nameof(NextVideo));
                    }
                }
            }
            finally
            {
                NowLoading = false;
                RaisePropertyChanged(nameof(NowLoading));
            }
        }
    }
}
