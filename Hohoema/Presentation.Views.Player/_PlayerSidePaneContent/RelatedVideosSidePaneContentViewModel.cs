using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase.Niconico.Player;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.VideoListPage;
using NiconicoToolkit;
using NiconicoToolkit.Recommend;
using NiconicoToolkit.Video;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;

namespace Hohoema.Presentation.ViewModels.Player.PlayerSidePaneContent
{
    public sealed class RelatedVideosSidePaneContentViewModel : SidePaneContentViewModelBase, IDisposable
    {
        public RelatedVideosSidePaneContentViewModel(
            NiconicoSession niconicoSession,
            PageManager pageManager,
            RelatedVideoContentsAggregator relatedVideoContentsAggregator,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand
           )
        {
            _niconicoSession = niconicoSession;
            _pageManager = pageManager;
            _relatedVideoContentsAggregator = relatedVideoContentsAggregator;
            VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
            OpenMylistCommand = _pageManager.OpenPageCommand;
        }

        private readonly NiconicoSession _niconicoSession;
        private readonly PageManager _pageManager;
        private readonly RelatedVideoContentsAggregator _relatedVideoContentsAggregator;

        public DelegateCommand<object> OpenMylistCommand { get; }
        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }

        public List<VideoListItemControlViewModel> Videos { get; private set; }

        public VideoListItemControlViewModel CurrentVideo { get; private set; }
        public VideoListItemControlViewModel NextVideo { get; private set; }

        public string JumpVideoId { get; set; }
        public bool HasVideoDescription { get; private set; }

        public Models.Helpers.AsyncLock _InitializeLock = new Models.Helpers.AsyncLock();

        private bool _IsInitialized = false;

        public bool NowLoading { get; private set; }

        public override void Dispose()
        {
            CurrentVideo?.Dispose();
            NextVideo?.Dispose();
            base.Dispose();
        }

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
            NowLoading = true;
            RaisePropertyChanged(nameof(NowLoading));
            try
            {
                using (var releaser = await _InitializeLock.LockAsync())
                {
                    if (_IsInitialized) { return; }
                    _IsInitialized = true;

                    var result = await _relatedVideoContentsAggregator.GetRelatedContentsAsync(currentVideo);

                    CurrentVideo?.Dispose();
                    CurrentVideo = new VideoListItemControlViewModel(currentVideo);
                    RaisePropertyChanged(nameof(CurrentVideo));

                    Videos = result.OtherVideos;
                    RaisePropertyChanged(nameof(Videos));

                    if (currentVideo.Series?.Video.Next is not null and NvapiVideoItem nextSeriesVideo)
                    {
                        NextVideo?.Dispose();
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
