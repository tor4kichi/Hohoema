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
using Windows.System;
using CommunityToolkit.Diagnostics;
using System.Windows.Input;

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
    private readonly DispatcherQueue _dispatcherQueue;
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
            UpdateLastCheckedAt();
            ResetList();
        }
        _lastSelectedSubscGroup = value;
        AllCheckedLocalizedTextForSelectedGroup = "SubscGroupVideosAllCheckedWithSubscGroupTitle".Translate(value?.Name ?? "All".Translate());
    }

    [ObservableProperty]
    private string _allCheckedLocalizedTextForSelectedGroup;

    [ObservableProperty]
    private bool _isDisplayChecked;


    partial void OnIsDisplayCheckedChanged(bool value)
    {
        ResetList();
    }

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
        _selectedSubscGroup = null;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    [ObservableProperty]
    private DateTime _lastCheckedAt;

    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        base.OnNavigatedTo(parameters);

        SubscriptionGroups.Clear();
        SubscriptionGroups.Add(null);
        SubscriptionGroups.Add(_subscriptionManager.DefaultSubscriptionGroup);
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

        if (SelectedSubscGroup != null)
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
            _dispatcherQueue.TryEnqueue(() =>
            {
                Guard.IsNotNull(ItemsView);

                var feed = m.Value;
                VideoId videoId = feed.VideoId;
                if (ItemsView.Cast<SubscVideoListItemViewModel>().FirstOrDefault(x => x.VideoId == videoId) is not null)
                {
                    return;
                }

                if (SelectedSubscGroup == null
                    || !_subscriptionManager.IsContainSubscriptionGroup(m.Value.SourceSubscId, SelectedSubscGroup!.GroupId)
                    )
                {
                    return;
                }

                var nicoVideo = _nicoVideoProvider.GetCachedVideoInfo(videoId);
                var itemVM = new SubscVideoListItemViewModel(feed, nicoVideo, _subscriptionManager.GetSubscription(m.Value.SourceSubscId), _subscriptionManager, CreatePlaylist());
                ItemsView.Insert(0, itemVM);
            });
        });

        _messenger.Register<SubscriptionGroupCheckedAtChangedMessage>(this, (r, m) => 
        {
            if (SelectedSubscGroup?.GroupId == m.SubscriptionGroupId)
            {
                UpdateLastCheckedAt();
                ResetList();
            }
        });
    }    


    private void UpdateLastCheckedAt()
    {
        if (SelectedSubscGroup != null)
        {
            LastCheckedAt = _subscriptionManager.GetLastCheckedAt(SelectedSubscGroup.GroupId);
        }
        else
        {
            LastCheckedAt = DateTime.MinValue;
        }
    }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        _messenger.Unregister<SubscFeedVideoValueChangedMessage>(this);
        _messenger.Unregister<NewSubscFeedVideoMessage>(this);
        _messenger.Unregister<SubscriptionGroupCheckedAtChangedMessage>(this);        
    }

    private SubscriptionGroupPlaylist CreatePlaylist()
    {        
        return new SubscriptionGroupPlaylist(SelectedSubscGroup, _subscriptionManager, _nicoVideoProvider, _localizeService);
    }

    protected override (int PageSize, IIncrementalSource<object> IncrementalSource) GenerateIncrementalSource()
    {
        return (
            SubscVideoListIncrementalLoadingSource.PageSize,
            new SubscVideoListIncrementalLoadingSource(SelectedSubscGroup, CreatePlaylist(), _subscriptionManager, _nicoVideoProvider, _messenger, SeparatorFactory) 
            {
                LastCheckedAt = LastCheckedAt ,
                IsDisplayChceked = IsDisplayChecked
            }
            );
    }

    private SubscVideoSeparatorListItemViewModel SeparatorFactory(IVideoContent lastContent)
    {
        var playlistItemToken = new PlaylistItemToken(CreatePlaylist(), SubscriptionGroupPlaylist.DefaultSortOption, lastContent);
        return new SubscVideoSeparatorListItemViewModel(LastCheckedAt, _messenger, playlistItemToken, allMarkAsCheckedAction: () => MarkAsCheckedWithDays(0));
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
            if (SelectedSubscGroup != null)
            {
                LastCheckedAt = _subscriptionManager.GetLatestPostAt(SelectedSubscGroup.GroupId) + TimeSpan.FromSeconds(1);
            }            
            else
            {
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
        
        if (SelectedSubscGroup == null)
        {
            // 購読グループ未指定の場合は全ての購読グループのチェック日時を設定する
            foreach (var groupId in _subscriptionManager.GetSubscGroups().Select(x => x.GroupId).Concat(new[] { SubscriptionGroupId.DefaultGroupId }))
            {
                var latestPostAt = _subscriptionManager.GetLatestPostAt(groupId);
                _subscriptionManager.SetCheckedAt(groupId, latestPostAt);

                // 指定日時以前の動画を全て視聴済みにマークする
                foreach (var video in _subscriptionManager.GetSubscFeedVideosOlderAt(SelectedSubscGroup?.GroupId, latestPostAt))
                {
                    VideoId videoId = video.VideoId;
                    _videoWatchedRepository.MarkWatched(videoId);
                    _messenger.Send(new VideoWatchedMessage(videoId));
                }
            }
        }
        else
        {
            _subscriptionManager.SetCheckedAt(SelectedSubscGroup.GroupId, LastCheckedAt);

            // 指定日時以前の動画を全て視聴済みにマークする
            foreach (var video in _subscriptionManager.GetSubscFeedVideosOlderAt(SelectedSubscGroup.GroupId, LastCheckedAt))
            {
                VideoId videoId = video.VideoId;
                _videoWatchedRepository.MarkWatched(videoId);
                _messenger.Send(new VideoWatchedMessage(videoId));
            }
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
    private readonly Func<IVideoContent, SubscVideoSeparatorListItemViewModel> _separatorFactory;
    public const int PageSize = 20;

    public SubscVideoListIncrementalLoadingSource(
        SubscriptionGroup? subscriptionGroup,
        SubscriptionGroupPlaylist subscriptionGroupPlaylist,
        SubscriptionManager subscriptionManager,
        NicoVideoProvider nicoVideoProvider,
        IMessenger messenger,
        Func<IVideoContent, SubscVideoSeparatorListItemViewModel> separatorFactory
        )
    {
        SubscriptionGroup = subscriptionGroup;
        _subscriptionGroupPlaylist = subscriptionGroupPlaylist;
        _subscriptionManager = subscriptionManager;
        _nicoVideoProvider = nicoVideoProvider;
        _messenger = messenger;
        _separatorFactory = separatorFactory;
    }
    
    public DateTime LastCheckedAt { get; set; }
    public bool IsDisplayChceked { get; set; }


    private readonly HashSet<VideoId> _videoIds = new HashSet<VideoId>();

    public SubscriptionGroup? SubscriptionGroup { get; }

    Dictionary<SubscriptionId, Models.Subscriptions.Subscription> _subscMap = new Dictionary<SubscriptionId, Models.Subscriptions.Subscription>();
    bool isCheckedSeparatorInserted = false;
    public Task<IEnumerable<object>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        IEnumerable<SubscFeedVideo> videos;
        if (!IsDisplayChceked)
        {
            videos = _subscriptionManager.GetSubscFeedVideosNewerAt(SubscriptionGroup?.GroupId, LastCheckedAt, pageIndex * pageSize, pageSize);
        }
        else
        {
            videos = _subscriptionManager.GetSubscFeedVideos(SubscriptionGroup?.GroupId, pageIndex * pageSize, pageSize);
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
                    resultItems.Add(_separatorFactory((resultItems.Last() as IVideoContent)!));
                }
            }

            var subscription = GetSubscription(video.SourceSubscId);
            _videoIds.Add(videoId);
            var nicoVideo = _nicoVideoProvider.GetCachedVideoInfo(videoId);
            resultItems.Add(new SubscVideoListItemViewModel(video, nicoVideo, subscription, _subscriptionManager, _subscriptionGroupPlaylist));
        }

        if (resultItems.Any() && isCheckedSeparatorInserted is false)
        {
            resultItems.Add(_separatorFactory((resultItems.Last() as IVideoContent)!));
        }

        return Task.FromResult<IEnumerable<object>>(resultItems);
    }

    Models.Subscriptions.Subscription GetSubscription(SubscriptionId subscriptionId)
    {
        if (!_subscMap.TryGetValue(subscriptionId, out var subsription))
        {
            subsription = _subscriptionManager.GetSubscription(subscriptionId);
            _subscMap.Add(subscriptionId, subsription);
        }

        return subsription;
    }
}

public sealed partial class SubscVideoSeparatorListItemViewModel : IPlaylistItemPlayable
{
    private readonly IMessenger _messenger;
    private readonly PlaylistItemToken _subscriptionGroupPlaylistItemToken;
    private readonly Action _allMarkAsCheckedAction;

    public SubscVideoSeparatorListItemViewModel(
        DateTime checkedDate,
        IMessenger messenger,
        PlaylistItemToken subscriptionGroupPlaylistItemToken,
        Action allMarkAsCheckedAction
        )
    {
        CheckedDate = checkedDate;
        _messenger = messenger;
        _subscriptionGroupPlaylistItemToken = subscriptionGroupPlaylistItemToken;
        _allMarkAsCheckedAction = allMarkAsCheckedAction;
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


    [RelayCommand]
    void PlayFromHere()
    {
        _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(_subscriptionGroupPlaylistItemToken));
    }

    [RelayCommand]
    void AllMarkAsChecked()
    {
        _allMarkAsCheckedAction();
    }
}

public sealed partial class SubscVideoListItemViewModel
    : VideoListItemControlViewModel
    , IPlaylistItemPlayable
{
    public SubscVideoListItemViewModel(
        SubscFeedVideo feedVideo,
        NicoVideo video,
        Models.Subscriptions.Subscription subscription,
        SubscriptionManager subscriptionManager,
        SubscriptionGroupPlaylist subscriptionGroupPlaylist
        ) : base(video)
    {
        FeedVideo = feedVideo;
        _subscription = subscription;        
        _subscriptionManager = subscriptionManager;
        _subscriptionGroupPlaylist = subscriptionGroupPlaylist;
        PlaylistItemToken = new PlaylistItemToken(_subscriptionGroupPlaylist, SubscriptionGroupPlaylist.DefaultSortOption, this);
    }

    public SubscFeedVideo FeedVideo { get; }

    private readonly Models.Subscriptions.Subscription _subscription;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly SubscriptionGroupPlaylist _subscriptionGroupPlaylist;    

    public (string id, SubscriptionSourceType sourceType, string? label) GetSubscriptionParameter()
    {
        return (_subscription.SourceParameter, _subscription.SourceType, _subscription.Label);
    }
}
