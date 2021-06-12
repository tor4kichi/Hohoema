﻿using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Models.UseCase.NicoVideos.Player;
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

namespace Hohoema.Presentation.ViewModels.Player.PlayerSidePaneContent
{
    public sealed class RelatedVideosSidePaneContentViewModel : SidePaneContentViewModelBase
    {
        public RelatedVideosSidePaneContentViewModel(
            NiconicoSession niconicoSession,
            HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            RelatedVideoContentsAggregator relatedVideoContentsAggregator
           )
        {
            _niconicoSession = niconicoSession;
            _hohoemaPlaylist = hohoemaPlaylist;
            _pageManager = pageManager;
            _relatedVideoContentsAggregator = relatedVideoContentsAggregator;
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
        private readonly RelatedVideoContentsAggregator _relatedVideoContentsAggregator;

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
            NowLoading = true;
            RaisePropertyChanged(nameof(NowLoading));
            try
            {
                using (var releaser = await _InitializeLock.LockAsync())
                {
                    if (_IsInitialized) { return; }
                    _IsInitialized = true;

                    var result = await _relatedVideoContentsAggregator.GetRelatedContentsAsync(currentVideo);

                    CurrentVideo = new VideoListItemControlViewModel(currentVideo);
                    RaisePropertyChanged(nameof(CurrentVideo));

                    Videos = result.OtherVideos;
                    RaisePropertyChanged(nameof(Videos));

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
