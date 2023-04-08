using CommunityToolkit.Mvvm.Input;
using Hohoema.Models.Niconico;
using Hohoema.Models.Player.Video;
using Hohoema.Services.Player;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.VideoListPage;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Player.PlayerSidePaneContent;

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

    public RelayCommand<object> OpenMylistCommand { get; }
    public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }

    public List<VideoListItemControlViewModel> Videos { get; private set; }

    public VideoListItemControlViewModel CurrentVideo { get; private set; }
    public VideoListItemControlViewModel NextVideo { get; private set; }

    public string JumpVideoId { get; set; }
    public bool HasVideoDescription { get; private set; }

    public Helpers.AsyncLock _InitializeLock = new Helpers.AsyncLock();

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
        OnPropertyChanged(nameof(CurrentVideo));

        NextVideo?.Dispose();
        NextVideo = null;
        OnPropertyChanged(nameof(NextVideo));


        if (Videos != null)
        {
            foreach (var item in Videos)
            {
                item.Dispose();
            }
            Videos.Clear();
            OnPropertyChanged(nameof(Videos));
        }

        _IsInitialized = false;
    }

    public async Task InitializeRelatedVideos(INicoVideoDetails currentVideo)
    {
        NowLoading = true;
        OnPropertyChanged(nameof(NowLoading));
        try
        {
            using (var releaser = await _InitializeLock.LockAsync())
            {
                if (_IsInitialized) { return; }
                _IsInitialized = true;

                var result = await _relatedVideoContentsAggregator.GetRelatedContentsAsync(currentVideo);

                CurrentVideo?.Dispose();
                CurrentVideo = new VideoListItemControlViewModel(currentVideo);
                OnPropertyChanged(nameof(CurrentVideo));

                Videos = result.OtherVideos;
                OnPropertyChanged(nameof(Videos));

                if (currentVideo.Series?.Video.Next is not null and NvapiVideoItem nextSeriesVideo)
                {
                    NextVideo?.Dispose();
                    NextVideo = new VideoListItemControlViewModel(nextSeriesVideo);
                    OnPropertyChanged(nameof(NextVideo));
                }
            }
        }
        finally
        {
            NowLoading = false;
            OnPropertyChanged(nameof(NowLoading));
        }
    }
}
