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
using Hohoema.Services.Player.Events;
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

namespace Hohoema.ViewModels.Pages.Hohoema.Subscription;

public partial class SubscVideoListPageViewModel : HohoemaListingPageViewModelBase<object>
{
    private readonly IMessenger _messenger;
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
        _pageManager = pageManager;
        _subscriptionManager = subscriptionManager;
        _nicoVideoProvider = nicoVideoProvider;
        _videoWatchedRepository = videoWatchedRepository;
        VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
        ApplicationLayoutManager = applicationLayoutManager;
        SelectionModeToggleCommand = selectionModeToggleCommand;
        SubscriptionGroups = new (_subscriptionManager.GetSubscGroups());
        _defaultSubscGroup = new SubscriptionGroup(ObjectId.Empty, "SubscGroup_DefaultGroupName".Translate());
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
            if (parameters.TryGetValue("SubscGroupId", out string idStr))
            {
                ObjectId subscriptionId = new ObjectId(idStr);
                if (SubscriptionGroups.Skip(1).FirstOrDefault(x => x.GroupId == subscriptionId) is not null and var group)
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
            var itemVM = new SubscVideoListItemViewModel(feed, nicoVideo, _subscriptionManager);
            ItemsView.Insert(0, itemVM);
        });
    }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        _messenger.Unregister<SubscFeedVideoValueChangedMessage>(this);
        _messenger.Unregister<NewSubscFeedVideoMessage>(this);
    }

    protected override (int PageSize, IIncrementalSource<object> IncrementalSource) GenerateIncrementalSource()
    {
        return (SubscVideoListIncrementalLoadingSource.PageSize, new SubscVideoListIncrementalLoadingSource(SelectedSubscGroup, _subscriptionManager, _nicoVideoProvider) { LastCheckedAt = LastCheckedAt });
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
                LastCheckedAt = _subscriptionManager.GetLastUpdatedAt(SelectedSubscGroup.GroupId);
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
    private readonly SubscriptionManager _subscriptionManager;
    private readonly NicoVideoProvider _nicoVideoProvider;
    public const int PageSize = 20;

    public SubscVideoListIncrementalLoadingSource(
        SubscriptionGroup? subscriptionGroup,
        SubscriptionManager subscriptionManager,
        NicoVideoProvider nicoVideoProvider
        )
    {
        SubscriptionGroup = subscriptionGroup;
        _subscriptionManager = subscriptionManager;
        _nicoVideoProvider = nicoVideoProvider;
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
            videos = SubscriptionGroup.GroupId != ObjectId.Empty
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
                resultItems.Add(new SubscVideoSeparatorListItemViewModel(LastCheckedAt));
            }

            _videoIds.Add(videoId);
            var nicoVideo = _nicoVideoProvider.GetCachedVideoInfo(videoId);
            resultItems.Add(new SubscVideoListItemViewModel(video, nicoVideo, _subscriptionManager));
        }

        return Task.FromResult<IEnumerable<object>>(resultItems);
    }
}

public sealed class SubscVideoSeparatorListItemViewModel
{
    public SubscVideoSeparatorListItemViewModel(DateTime checkedDate)
    {
        CheckedDate = checkedDate;
    }

    public DateTime CheckedDate { get; }

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

public sealed partial class SubscVideoListItemViewModel : VideoListItemControlViewModel
{
    public SubscVideoListItemViewModel(SubscFeedVideo feedVideo, NicoVideo video, SubscriptionManager subscriptionManager) : base(video)
    {
        FeedVideo = feedVideo;
        _subscriptionManager = subscriptionManager;
    }

    public SubscFeedVideo FeedVideo { get; }

    private readonly SubscriptionManager _subscriptionManager;
}
