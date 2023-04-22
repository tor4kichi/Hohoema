#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Navigations;
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
using Hohoema.Models.PageNavigation;

namespace Hohoema.ViewModels.Pages.Hohoema.Subscription;

public sealed partial class SubscVideoListPageViewModel : HohoemaPageViewModelBase
{
    private readonly IMessenger _messenger;
    private readonly ILocalizeService _localizeService;
    private readonly INotificationService _notificationService;
    private readonly PageManager _pageManager;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly QueuePlaylist _queuePlaylist;
    private readonly VideoWatchedRepository _videoWatchedRepository;
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
        INotificationService notificationService,
        PageManager pageManager,
        SubscriptionManager subscriptionManager,
        NicoVideoProvider nicoVideoProvider,
        QueuePlaylist queuePlaylist,
        VideoWatchedRepository videoWatchedRepository,
        VideoPlayWithQueueCommand videoPlayWithQueueCommand,
        ApplicationLayoutManager applicationLayoutManager,
        SelectionModeToggleCommand selectionModeToggleCommand
        )
        : base()
    {
        _messenger = messenger;
        _localizeService = localizeService;
        _notificationService = notificationService;
        _pageManager = pageManager;
        _subscriptionManager = subscriptionManager;
        _nicoVideoProvider = nicoVideoProvider;
        _queuePlaylist = queuePlaylist;
        _videoWatchedRepository = videoWatchedRepository;
        VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
        ApplicationLayoutManager = applicationLayoutManager;
        SelectionModeToggleCommand = selectionModeToggleCommand;
        SubscriptionGroups = new (_subscriptionManager.GetSubscriptionGroups());
        _selectedSubscGroup = null;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    [ObservableProperty]
    private bool _nowLoading;

    public ObservableCollection<SubscriptionNewVideosViewModel> SubscVideosItems { get; } = new();

    [RelayCommand]
    public void ResetList()
    {
        NowLoading = true;
        try
        {
            ClearList();
            if (SelectedSubscGroup == null) { return; }

            var groupPlaylist = CreatePlaylist();
            foreach (var subsc in _subscriptionManager.GetSubscriptions(SelectedSubscGroup.GroupId))
            {
                var groupVideosVM = new SubscriptionNewVideosViewModel(
                    subsc, 
                    _subscriptionManager, 
                    _nicoVideoProvider,
                    _queuePlaylist,
                    _messenger,
                    _notificationService,
                    groupPlaylist
                    );
                SubscVideosItems.Add(groupVideosVM);
            }
        }
        finally
        {
            NowLoading = false;
            RefreshHasNewVideos();
        }       
    }

    private void ClearList()
    {
        foreach (var subscVideosVM in SubscVideosItems)
        {
            //(subscVideosVM as IDisposable)?.Dispose();
        }

        SubscVideosItems.Clear();
        HasNewVideos = false;
    }


    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        base.OnNavigatedTo(parameters);

        SubscriptionGroups.Clear();
        SubscriptionGroups.Add(null);
        SubscriptionGroups.Add(_subscriptionManager.DefaultSubscriptionGroup);
        foreach (var subscGroup in _subscriptionManager.GetSubscriptionGroups())
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

        ResetList();

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
                var feed = m.Value;
                VideoId videoId = feed.VideoId;
                if (SelectedSubscGroup == null
                    || !_subscriptionManager.IsContainSubscriptionGroup(m.Value.SourceSubscId, SelectedSubscGroup!.GroupId)
                    )
                {
                    return;
                }

                if (SubscVideosItems.FirstOrDefault(x => x.Subscription.SubscriptionId == feed.SourceSubscId) is not { } subscVideos)
                {
                    return;
                }

                var nicoVideo = _nicoVideoProvider.GetCachedVideoInfo(videoId);
                var itemVM = new SubscVideoListItemViewModel(feed, nicoVideo, _subscriptionManager.GetSubscription(m.Value.SourceSubscId), _subscriptionManager, CreatePlaylist());
                subscVideos.Items.Insert(0, itemVM);
                RefreshHasNewVideos();
            });
        });

        _messenger.Register<SubscriptionCheckedAtChangedMessage>(this, (r, m) => 
        {
            var feed = m.Value;
            if (SelectedSubscGroup?.GroupId == m.SubscriptionGroupId)
            {
                if (SubscVideosItems.FirstOrDefault(x => x.Subscription.SubscriptionId == feed.SubscriptionSourceId) is not { } subscVideos)
                {
                    return;
                }

                subscVideos.RefreshItems();
                RefreshHasNewVideos();
            }
        });
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MarkAsCheckedWithDaysCommand))]
    bool _hasNewVideos;

    void RefreshHasNewVideos()
    {
        HasNewVideos = SubscVideosItems.Any(x => x.HasNewVideos);
    }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        _messenger.Unregister<SubscFeedVideoValueChangedMessage>(this);
        _messenger.Unregister<NewSubscFeedVideoMessage>(this);
        _messenger.Unregister<SubscriptionCheckedAtChangedMessage>(this);        
    }

    private SubscriptionGroupPlaylist CreatePlaylist()
    {        
        return new SubscriptionGroupPlaylist(SelectedSubscGroup, _subscriptionManager, _nicoVideoProvider, _localizeService);
    }


    [RelayCommand]
    public void OpenSubscManagementPage()
    {
        _pageManager.OpenPage(Models.PageNavigation.HohoemaPageType.SubscriptionManagement);
    }

    [RelayCommand(CanExecute = nameof(HasNewVideos))]
    public void MarkAsCheckedWithDays(int days)
    {                
        if (SelectedSubscGroup == null)
        {
            DateTime checkedAt;
            if (days == 0)
            {
                checkedAt = DateTime.Now;
            }
            else
            {
                var targetDateTime = DateTime.Now - TimeSpan.FromDays(days);
                checkedAt = targetDateTime;
            }

            // 購読グループ未指定の場合は全ての購読グループのチェック日時を設定する
            foreach (var groupId in _subscriptionManager.GetSubscriptionGroups().Select(x => x.GroupId).Concat(new[] { SubscriptionGroupId.DefaultGroupId }))
            {
                // 指定日時以前の動画を全て視聴済みにマークする
                foreach (var video in _subscriptionManager.GetSubscFeedVideosOlderAt(groupId, checkedAt))
                {
                    VideoId videoId = video.VideoId;
                    _videoWatchedRepository.MarkWatched(videoId);
                    _messenger.Send(new VideoWatchedMessage(videoId));
                }

                _subscriptionManager.UpdateSubscriptionCheckedAt(groupId, checkedAt);
            }
        }
        else
        {
            DateTime checkedAt;
            if (days == 0)
            {
                checkedAt = _subscriptionManager.GetLatestPostAt(SelectedSubscGroup.GroupId) + TimeSpan.FromSeconds(1);
            }
            else
            {
                var targetDateTime = DateTime.Now - TimeSpan.FromDays(days);
                checkedAt = targetDateTime;
            }
            
            // 指定日時以前の動画を全て視聴済みにマークする
            foreach (var video in _subscriptionManager.GetSubscFeedVideosOlderAt(SelectedSubscGroup.GroupId, checkedAt))
            {
                VideoId videoId = video.VideoId;
                _videoWatchedRepository.MarkWatched(videoId);
                _messenger.Send(new VideoWatchedMessage(videoId));
            }

            _subscriptionManager.UpdateSubscriptionCheckedAt(SelectedSubscGroup.GroupId, checkedAt);

        }

        ResetList();
    }
}

public sealed partial class SubscriptionNewVideosViewModel : ObservableObject, IPlaylistItemPlayable
{
    private readonly SubscriptionManager _subscriptionManager;
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly QueuePlaylist _queuePlaylist;

    public ObservableCollection<SubscVideoListItemViewModel> Items { get; } = new();
    public Models.Subscriptions.Subscription Subscription { get; }

    public SubscriptionNewVideosViewModel(
        Models.Subscriptions.Subscription subscription,
        SubscriptionManager subscriptionManager,
        NicoVideoProvider nicoVideoProvider,
        QueuePlaylist queuePlaylist,
        IMessenger messenger,
        INotificationService notificationService,
        SubscriptionGroupPlaylist subscriptionGroupPlaylist
        )
    {
        Subscription = subscription;
        _subscriptionManager = subscriptionManager;
        _nicoVideoProvider = nicoVideoProvider;
        _queuePlaylist = queuePlaylist;
        _messenger = messenger;
        _notificationService = notificationService;
        _subscriptionGroupPlaylist = subscriptionGroupPlaylist;

        RefreshItems();
    }

    private readonly IMessenger _messenger;
    private readonly INotificationService _notificationService;
    private readonly SubscriptionGroupPlaylist _subscriptionGroupPlaylist;
    private readonly PlaylistItemToken _subscriptionGroupPlaylistItemToken;    

    public PlaylistItemToken? PlaylistItemToken => _subscriptionGroupPlaylistItemToken;


    [ObservableProperty]
    private DateTime _lastCheckedAt;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayFromHereCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddToQueueCommand))]
    [NotifyCanExecuteChangedFor(nameof(AllMarkAsCheckedCommand))]
    private bool _hasNewVideos;

    internal void RefreshItems()
    {
        Items.Clear();

        LastCheckedAt = _subscriptionManager.GetSubscriptionCheckedAt(Subscription.SubscriptionId);
        foreach (var video in _subscriptionManager.GetSubscFeedVideosNewerAt(Subscription, _lastCheckedAt))
        {
            Items.Add(new SubscVideoListItemViewModel(video, _nicoVideoProvider.GetCachedVideoInfo(video.VideoId), Subscription, _subscriptionManager, _subscriptionGroupPlaylist));
        }

        HasNewVideos = Items.Any();
    }

    [RelayCommand(CanExecute = nameof(HasNewVideos))]
    void PlayFromHere()
    {
        if (Items.Any() is false) { return; }

        _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(_subscriptionGroupPlaylistItemToken));
    }

    [RelayCommand(CanExecute = nameof(HasNewVideos))]
    void AllMarkAsChecked()
    {
        _subscriptionManager.UpdateSubscriptionCheckedAt(Subscription);
        Items.Clear();
        HasNewVideos = Items.Any();
    }

    [RelayCommand]
    async Task OpenVideoListPage()
    {
        (HohoemaPageType pageType, string param) = Subscription.SourceType switch
        {
            SubscriptionSourceType.Mylist => (HohoemaPageType.Mylist, $"id={Subscription.SourceParameter}"),
            SubscriptionSourceType.User => (HohoemaPageType.UserVideo, $"id={Subscription.SourceParameter}"),
            SubscriptionSourceType.Channel => (HohoemaPageType.ChannelVideo, $"id={Subscription.SourceParameter}"),
            SubscriptionSourceType.Series => (HohoemaPageType.Series, $"id={Subscription.SourceParameter}"),
            SubscriptionSourceType.SearchWithKeyword => (HohoemaPageType.SearchResultKeyword, $"keyword={Uri.EscapeDataString(Subscription.SourceParameter)}"),
            SubscriptionSourceType.SearchWithTag => (HohoemaPageType.SearchResultTag, $"keyword={Uri.EscapeDataString(Subscription.SourceParameter)}"),
            _ => throw new NotImplementedException(),
        };

        await _messenger.SendNavigationRequestAsync(pageType, new NavigationParameters(param));
    }

    [RelayCommand(CanExecute = nameof(HasNewVideos))]
    void AddToQueue()
    {
        if (Items.Any() is false) { return; }

        int count = Items.Count;
        foreach (var item in Items)
        {
            _queuePlaylist.Add(item);
        }

        AllMarkAsChecked();

        _notificationService.ShowLiteInAppNotification("Notification_SuccessAddToWatchLaterWithAddedCount".Translate(count));
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

    public SubscriptionSourceType SourceType => _subscription.SourceType;
    public string SourceLabel => _subscription.Label;

    public (string id, SubscriptionSourceType sourceType, string? label) GetSubscriptionParameter()
    {
        return (_subscription.SourceParameter, _subscription.SourceType, _subscription.Label);
    }
}
