#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.ViewModels;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using Hohoema.Services;
using Hohoema.Services.Niconico;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.VideoListPage;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Activity.VideoWatchHistory;
using NiconicoToolkit.Video;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using ZLogger;

namespace Hohoema.ViewModels.Pages.Niconico.Activity;

public enum VideoContentType
{
    Video,
    ShortVideo
}

public sealed partial class WatchHistoryPageViewModel 
    : VideoListingPageViewModelBase<HistoryVideoListItemControlViewModel>
{
    public WatchHistoryPageViewModel(
        IMessenger messenger,
        ILoggerFactory loggerFactory,
        ApplicationLayoutManager applicationLayoutManager,        
        NiconicoSession niconicoSession,
        WatchHistoryManager watchHistoryManager,    
        VideoPlayWithQueueCommand videoPlayWithQueueCommand,
        WatchHistoryRemoveAllCommand watchHistoryRemoveAllCommand,
        SelectionModeToggleCommand selectionModeToggleCommand
        )
        : base(messenger, loggerFactory.CreateLogger<WatchHistoryPageViewModel>(), disposeItemVM: false)
    {
        ApplicationLayoutManager = applicationLayoutManager;
        _niconicoSession = niconicoSession;
        _watchHistoryManager = watchHistoryManager;        
        VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
        WatchHistoryRemoveAllCommand = watchHistoryRemoveAllCommand;
        SelectionModeToggleCommand = selectionModeToggleCommand;
      
    }

    private readonly NiconicoSession _niconicoSession;
    private readonly WatchHistoryManager _watchHistoryManager;
    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
    public WatchHistoryRemoveAllCommand WatchHistoryRemoveAllCommand { get; }
    public SelectionModeToggleCommand SelectionModeToggleCommand { get; }

    public VideoContentType[] VideoContentTypeItems { get; } = [VideoContentType.Video, VideoContentType.ShortVideo];

    [ObservableProperty]
    VideoContentType _selectedVideoContentType = VideoContentType.Video;

    partial void OnSelectedVideoContentTypeChanged(VideoContentType value)
    {
        ResetList();
    }

    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        Observable.FromEventPattern<WatchHistoryRemovedEventArgs>(
            h => _watchHistoryManager.WatchHistoryRemoved += h,
            h => _watchHistoryManager.WatchHistoryRemoved -= h
            )
            .Subscribe(e =>
            {
                var args = e.EventArgs;
                var removedItem = ItemsView.Cast<HistoryVideoListItemControlViewModel>().FirstOrDefault(x => x.VideoId == args.VideoId);
                if (removedItem != null)
                {
                    ItemsView.Remove(removedItem);
                }
            })
            .AddTo(_CompositeDisposable);

        Observable.FromEventPattern(
            h => _watchHistoryManager.WatchHistoryAllRemoved += h,
            h => _watchHistoryManager.WatchHistoryAllRemoved -= h
            )
            .Subscribe(_ =>
            {
                ItemsView!.Clear();
            })
            .AddTo(_CompositeDisposable);

        base.OnNavigatedTo(parameters);
    }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        base.OnNavigatedFrom(parameters);
    }

    protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode, INavigationParameters parameters)
    {
        return base.CheckNeedUpdateOnNavigateTo(mode, parameters);
    }

    protected override (int PageSize, Microsoft.Toolkit.Collections.IIncrementalSource<HistoryVideoListItemControlViewModel> IncrementalSource) GenerateIncrementalSource()
    {
        return (25, new HistoryVideoListItemIncrementalLoadingSource(_niconicoSession, SelectedVideoContentType));
    }    
}




public class HistoryVideoListItemControlViewModel : VideoListItemControlViewModel, IWatchHistory
{
    public DateTime LastWatchedAt { get; }
    public uint UserViewCount { get; }

    public HistoryVideoListItemControlViewModel(DateTime lastWatchedAt, uint viewCount, string rawVideoId, string title, string thumbnailUrl, TimeSpan videoLength, DateTime postedAt)
        : base(rawVideoId, title, thumbnailUrl, videoLength, postedAt)
    {
        LastWatchedAt = lastWatchedAt;
        UserViewCount = viewCount;
    }
}

public class HistoryVideoListItemIncrementalLoadingSource : IIncrementalSource<HistoryVideoListItemControlViewModel>
{
    private readonly NiconicoSession _niconicoSession;
    private readonly VideoContentType _videoContentType;

    public HistoryVideoListItemIncrementalLoadingSource(NiconicoSession niconicoSession, VideoContentType videoContentType)
    {
        _niconicoSession = niconicoSession;
        _videoContentType = videoContentType;
    }

    VideoWatchHistory? _lastRes;
    public async Task<IEnumerable<HistoryVideoListItemControlViewModel>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var res = _videoContentType == VideoContentType.Video 
            ? await _niconicoSession.ToolkitContext.History.VideoWachHistory.GetWatchHistoryAsync(6, _lastRes)
            : await _niconicoSession.ToolkitContext.History.VideoWachHistory.GetShortVideoWatchHistoryAsync(6, _lastRes);

        _lastRes = res;
        var items = res.Data.Items.Select(x => new HistoryVideoListItemControlViewModel(
            x.ViewedAt,
            0,
            x.Video.Id,
            x.Video.Title,
            x.Video.Thumbnail.ListingUrl.OriginalString,
            TimeSpan.FromSeconds(x.Video.Duration),
            x.Video.RegisteredAt.DateTime
            )
        {
            ProviderId = x.Video.Owner.Id,
            ProviderType = x.Video.Owner.OwnerType switch
            {
                NiconicoToolkit.Video.OwnerType.User => OwnerType.User,
                NiconicoToolkit.Video.OwnerType.Channel => OwnerType.Channel,
                _ => OwnerType.Hidden
            },
            ProviderName = x.Video.Owner.Name,
            CommentCount = x.Video.Count.Comment,
            ViewCount = x.Video.Count.View,
            MylistCount = x.Video.Count.Mylist,
        }).ToList();

        var videoWatchedRepository = Ioc.Default.GetRequiredService<VideoWatchedRepository>();
        foreach (VideoWatchHistoryDataItem history in res.Data.Items)
        {
            videoWatchedRepository.VideoPlayedIfNotWatched(history.Video.Id, TimeSpan.MaxValue);
        }

        return items;
    }
}
