#nullable enable
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
        IMessenger messenger,
        NiconicoSession niconicoSession,        
        RelatedVideoContentsAggregator relatedVideoContentsAggregator,
        VideoPlayWithQueueCommand videoPlayWithQueueCommand
       )
    {
        _messenger = messenger;
        _niconicoSession = niconicoSession;
        _relatedVideoContentsAggregator = relatedVideoContentsAggregator;
        VideoPlayWithQueueCommand = videoPlayWithQueueCommand;        
    }

    private readonly IMessenger _messenger;
    private readonly NiconicoSession _niconicoSession;    
    private readonly RelatedVideoContentsAggregator _relatedVideoContentsAggregator;

    
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
        base.Dispose();
    }

    public void Clear()
    {
        CurrentVideo = null;
        OnPropertyChanged(nameof(CurrentVideo));

        NextVideo = null;
        OnPropertyChanged(nameof(NextVideo));


        if (Videos != null)
        {
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

                CurrentVideo = new VideoListItemControlViewModel(currentVideo);
                OnPropertyChanged(nameof(CurrentVideo));

                Videos = result.OtherVideos;
                OnPropertyChanged(nameof(Videos));

                if (currentVideo.Series?.Video.Next is not null and NvapiVideoItem nextSeriesVideo)
                {
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

    private RelayCommand<object> _OpenMylistCommand;
    public RelayCommand<object> OpenMylistCommand =>
        _OpenMylistCommand ??= new RelayCommand<object>(item => 
        {
            //_messenger.OpenPageAsync()
        });
}
