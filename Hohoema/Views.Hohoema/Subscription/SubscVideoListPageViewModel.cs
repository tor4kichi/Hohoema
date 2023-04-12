#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Services.Navigations;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Subscriptions;
using Hohoema.Services;
using Hohoema.Services.Niconico;
using Hohoema.Contracts.Player;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.VideoListPage;
using I18NPortable;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hohoema.Models.Playlist;

namespace Hohoema.ViewModels.Pages.Hohoema.Subscription;

public partial class SubscVideoListPageViewModel : HohoemaListingPageViewModelBase<object>
{
    private readonly IMessenger _messenger;
    private readonly ILocalizeService _localizeService;
    private readonly PageManager _pageManager;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly VideoWatchedRepository _videoWatchedRepository;
    private readonly WatchHistoryManager _watchHistoryManager;

    public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    public SelectionModeToggleCommand SelectionModeToggleCommand { get; }

    public ObservableCollection<SubscriptionGroup?> SubscriptionGroups { get; }

    [ObservableProperty]
    private SubscriptionGroup? _selectedSubscGroup;

    partial void OnSelectedSubscGroupChanged(SubscriptionGroup? value)
    {
        if (_lastSelectedSubscGroup != value)
        {
            if (value != null && !value.IsInvalidId)
            {
                LastCheckedAt = _subscriptionManager.GetLastCheckedAt(value.GroupId);
            }
            else
            {
                LastCheckedAt = DateTime.MinValue;
            }

            ResetList();
        }
        _lastSelectedSubscGroup = value;
    }

    private SubscriptionGroup _defaultSubscGroup;
    private SubscriptionGroup? _lastSelectedSubscGroup;
    public SubscVideoListPageViewModel(
        ILogger logger,
        IMessenger messenger,
        ILocalizeService localizeService,
        PageManager pageManager,
        SubscriptionManager subscriptionManager,
        NicoVideoProvider nicoVideoProvider,
        VideoWatchedRepository videoWatchedRepository,
        VideoPlayWithQueueCommand videoPlayWithQueueCommand,
        ApplicationLayoutManager applicationLayoutManager,
        SelectionModeToggleCommand selectionModeToggleCommand
        )
        : base(logger)
    {
        _messenger = messenger;
        _localizeService = localizeService;
        _pageManager = pageManager;
        _subscriptionManager = subscriptionManager;
        _nicoVideoProvider = nicoVideoProvider;
        _videoWatchedRepository = videoWatchedRepository;
        VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
        ApplicationLayoutManager = applicationLayoutManager;
        SelectionModeToggleCommand = selectionModeToggleCommand;
        SubscriptionGroups = new (_subscriptionManager.GetSubscGroups());
        _defaultSubscGroup = new SubscriptionGroup(SubscriptionGroupId.DefaultGroupId, "SubscGroup_DefaultGroupName".Translate());
        _selectedSubscGroup = null;
    }

    [ObservableProperty]
    private DateTime _lastCheckedAt;

    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        base.OnNavigatedTo(parameters);

        SubscriptionGroups.Clear();
        SubscriptionGroups.Add(null);
        SubscriptionGroups.Add(_defaultSubscGroup);
        foreach (var subscGroup in _subscriptionManager.GetSubscGroups())
        {
            SubscriptionGroups.Add(subscGroup);
        }
        
        try
        {
            if (parameters.TryGetValue("SubscGroupId", out string idStr)
                && SubscriptionGroupId.TryParse(idStr, out SubscriptionGroupId subscriptionId)
                )
            {                
                if (SubscriptionGroups.Skip(1).FirstOrDefault(x => x!.GroupId == subscriptionId) is not null and var group)
                {
                    SelectedSubscGroup = group;
                }
                else
                {
                    SelectedSubscGroup = null;
                }                
            }
            else
            {
                SelectedSubscGroup = null;
            }
        }
        catch 
        {
            SelectedSubscGroup = null;
        }

        if (SelectedSubscGroup != null && !SelectedSubscGroup.IsInvalidId)
        {
            LastCheckedAt = _subscriptionManager.GetLastCheckedAt(SelectedSubscGroup.GroupId);
        }
        else
        {
            LastCheckedAt = DateTime.MinValue;
        }

        _messenger.Register<SubscFeedVideoValueChangedMessage>(this, (r, m) => 
        {
            //if (m.Value.IsChecked is false)
            //{
            //    var target = ItemsView.Cast<SubscVideoListItemViewModel>().FirstOrDefault(x => x.VideoId == m.Value.VideoId);
            //    if (target is not null)
            //    {
            //        ItemsView.Remove(target);
            //    }
            //}
        });

        _messenger.Register<NewSubscFeedVideoMessage>(this, (r, m) => 
        {
            var feed = m.Value;
            VideoId videoId = feed.VideoId;

            if (ItemsView.Cast<SubscVideoListItemViewModel>().FirstOrDefault(x => x.VideoId == videoId) is not null)
            {
                return;
            }

            var nicoVideo = _nicoVideoProvider.GetCachedVideoInfo(videoId);
            var itemVM = new SubscVideoListItemViewModel(feed, nicoVideo, _subscriptionManager, CreatePlaylist());
            ItemsView.Insert(0, itemVM);
        });
    }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        _messenger.Unregister<SubscFeedVideoValueChangedMessage>(this);
        _messenger.Unregister<NewSubscFeedVideoMessage>(this);
    }

    private SubscriptionGroupPlaylist CreatePlaylist()
    {
        return new SubscriptionGroupPlaylist(SelectedSubscGroup, _subscriptionManager, _nicoVideoProvider, _localizeService);
    }

    protected override (int PageSize, IIncrementalSource<object> IncrementalSource) GenerateIncrementalSource()
    {
        return (
            SubscVideoListIncrementalLoadingSource.PageSize,
            new SubscVideoListIncrementalLoadingSource(SelectedSubscGroup, CreatePlaylist(), _subscriptionManager, _nicoVideoProvider, _messenger) { LastCheckedAt = LastCheckedAt }
            );
    }


    [RelayCommand]
    public void OpenSubscManagementPage()
    {
        _pageManager.OpenPage(Models.PageNavigation.HohoemaPageType.SubscriptionManagement);
    }

    [RelayCommand]
    public void MarkAsCheckedWithDays(int days)
    {
        if (days == 0)
        {
            if (SelectedSubscGroup != null && !SelectedSubscGroup.IsInvalidId)
            {                
                LastCheckedAt = _subscriptionManager.GetLastCheckedAt(SelectedSubscGroup.GroupId);
            }            
            else
            {
                // 無指定時
                LastCheckedAt = DateTime.Now;
            }
        }
        else
        {
            var targetDateTime = DateTime.Now - TimeSpan.FromDays(days);
            if (targetDateTime < LastCheckedAt)
            {
                return;
            }

            LastCheckedAt = targetDateTime;
        }

        // 指定日時以前の動画を全て視聴済みにマークする
        var videos = SelectedSubscGroup != null
            ? _subscriptionManager.GetSubscFeedVideosForMarkAsChecked(SelectedSubscGroup, LastCheckedAt)
            : _subscriptionManager.GetSubscFeedVideosForMarkAsChecked(LastCheckedAt)
            ;

        foreach (var video in videos)
        {
            VideoId videoId = video.VideoId;
            _videoWatchedRepository.MarkWatched(videoId);
            _messenger.Send(new VideoWatchedMessage(videoId));
        }

        ResetList();
    }
}

public sealed class SubscVideoListIncrementalLoadingSource : IIncrementalSource<object>
{
    private readonly SubscriptionGroupPlaylist _subscriptionGroupPlaylist;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly IMessenger _messenger;
    public const int PageSize = 20;

    public SubscVideoListIncrementalLoadingSource(
        SubscriptionGroup? subscriptionGroup,
        SubscriptionGroupPlaylist subscriptionGroupPlaylist,
        SubscriptionManager subscriptionManager,
        NicoVideoProvider nicoVideoProvider,
        IMessenger messenger
        )
    {
        SubscriptionGroup = subscriptionGroup;
        _subscriptionGroupPlaylist = subscriptionGroupPlaylist;
        _subscriptionManager = subscriptionManager;
        _nicoVideoProvider = nicoVideoProvider;
        _messenger = messenger;
    }
    
    public DateTime LastCheckedAt { get; set; }

    private readonly HashSet<VideoId> _videoIds = new HashSet<VideoId>();

    public SubscriptionGroup? SubscriptionGroup { get; }

    bool isCheckedSeparatorInserted = false;
    public Task<IEnumerable<object>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        IEnumerable<SubscFeedVideo> videos;
        if (SubscriptionGroup != null)
        {
            videos = SubscriptionGroup.GroupId != SubscriptionGroupId.DefaultGroupId
                ? _subscriptionManager.GetSubscFeedVideos(SubscriptionGroup, pageIndex * pageSize, pageSize)
                : _subscriptionManager.GetSubscFeedVideos(default(SubscriptionGroup), pageIndex * pageSize, pageSize)
                ;
        }
        else
        {
            videos = _subscriptionManager.GetAllSubscFeedVideos(pageIndex * pageSize, pageSize);
        }
        
        List<object> resultItems = new();
        foreach (var video in videos)
        {
            VideoId videoId = video.VideoId;
            if (_videoIds.Contains(videoId))
            {
                continue;
            }            

            if (isCheckedSeparatorInserted is false
                && video.PostAt < LastCheckedAt
                )
            {
                isCheckedSeparatorInserted = true;

                if (resultItems.Any())
                {
                    var playlistItemToken = new PlaylistItemToken(_subscriptionGroupPlaylist, SubscriptionGroupPlaylist.DefaultSortOption, (resultItems.Last() as IVideoContent)!);
                    resultItems.Add(new SubscVideoSeparatorListItemViewModel(LastCheckedAt, _messenger, playlistItemToken));
                }
            }

            _videoIds.Add(videoId);
            var nicoVideo = _nicoVideoProvider.GetCachedVideoInfo(videoId);
            resultItems.Add(new SubscVideoListItemViewModel(video, nicoVideo, _subscriptionManager, _subscriptionGroupPlaylist));
        }

        return Task.FromResult<IEnumerable<object>>(resultItems);
    }
}

public sealed partial class SubscVideoSeparatorListItemViewModel : IPlaylistItemPlayable
{
    private readonly IMessenger _messenger;
    private readonly PlaylistItemToken _subscriptionGroupPlaylistItemToken;

    public SubscVideoSeparatorListItemViewModel(
        DateTime checkedDate,
        IMessenger messenger,
        PlaylistItemToken subscriptionGroupPlaylistItemToken
        )
    {
        CheckedDate = checkedDate;
        _messenger = messenger;
        _subscriptionGroupPlaylistItemToken = subscriptionGroupPlaylistItemToken;
    }

    public DateTime CheckedDate { get; }

    public PlaylistItemToken? PlaylistItemToken => _subscriptionGroupPlaylistItemToken;

    public string Localize(DateTime date)
    {
        if (date == DateTime.MinValue)
        {
            return $"ここまで視聴済み";
        }
        else if (date == DateTime.MaxValue)
        {
            return $"ここまで視聴済み {date:g}";
        }
        else
        {
            return $"ここまで視聴済み {date:g}";
        }        
    }    
}

public sealed partial class SubscVideoListItemViewModel
    : VideoListItemControlViewModel
    , IPlaylistItemPlayable
{
    public SubscVideoListItemViewModel(
        SubscFeedVideo feedVideo,
        NicoVideo video,
        SubscriptionManager subscriptionManager,
        SubscriptionGroupPlaylist subscriptionGroupPlaylist
        ) : base(video)
    {
        FeedVideo = feedVideo;
        _subscriptionManager = subscriptionManager;
        _subscriptionGroupPlaylist = subscriptionGroupPlaylist;
        PlaylistItemToken = new PlaylistItemToken(_subscriptionGroupPlaylist, SubscriptionGroupPlaylist.DefaultSortOption, this);
    }

    public SubscFeedVideo FeedVideo { get; }

    private readonly SubscriptionManager _subscriptionManager;
    private readonly SubscriptionGroupPlaylist _subscriptionGroupPlaylist;    
}
