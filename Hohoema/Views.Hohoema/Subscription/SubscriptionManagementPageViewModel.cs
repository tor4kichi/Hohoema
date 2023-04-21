#nullable enable
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Navigations;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Search;
using Hohoema.Models.Niconico.Series;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
using Hohoema.Models.User;
using Hohoema.Services;
using Hohoema.Services.Navigations;
using Hohoema.Services.Subscriptions;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.VideoListPage;
using I18NPortable;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.UI;
using NiconicoToolkit.Video;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using ZLogger;

namespace Hohoema.ViewModels.Pages.Hohoema.Subscription;


public partial class SubscriptionManagementPageViewModel : HohoemaPageViewModelBase, IRecipient<SettingsRestoredMessage>, IDisposable
{
    private readonly SubscriptionManager _subscriptionManager;
    private readonly SubscriptionUpdateManager _subscriptionUpdateManager;
    private readonly DialogService _dialogService;
    private readonly QueuePlaylist _queuePlaylist;
    private readonly IScheduler _scheduler;
    private readonly IMessenger _messenger;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SubscriptionManagementPageViewModel> _logger;

    public IReadOnlyReactiveProperty<bool> IsAutoUpdateRunning { get; }
    public IReactiveProperty<TimeSpan> AutoUpdateFrequency { get; }
    public IReactiveProperty<bool> IsAutoUpdateEnabled { get; }
    
    public ObservableCollection<SubscriptionGroupViewModel> SubscriptionGroups { get; } = new();
    public ApplicationLayoutManager ApplicationLayoutManager { get; }

    void IRecipient<SettingsRestoredMessage>.Receive(SettingsRestoredMessage message)
    {
        ResetSubscriptionGroups();
    }

    public SubscriptionManagementPageViewModel(
        ILoggerFactory loggerFactory,
        IScheduler scheduler, 
        IMessenger messenger,
        INotificationService notificationService,
        SubscriptionManager subscriptionManager,
        SubscriptionUpdateManager subscriptionUpdateManager,
        DialogService dialogService,
        PageManager pageManager,
        ApplicationLayoutManager applicationLayoutManager,
        QueuePlaylist queuePlaylist
        )
    {
        _logger = loggerFactory.CreateLogger<SubscriptionManagementPageViewModel>();
        _scheduler = scheduler;
        _messenger = messenger;
        _notificationService = notificationService;
        _subscriptionManager = subscriptionManager;
        _subscriptionUpdateManager = subscriptionUpdateManager;
        _dialogService = dialogService;        
        ApplicationLayoutManager = applicationLayoutManager;
        _queuePlaylist = queuePlaylist;
        _messenger.Register<SettingsRestoredMessage>(this);

        //Subscriptions = new ObservableCollection<SubscriptionViewModel>();
        //Subscriptions.CollectionChangedAsObservable()
        //   .Throttle(TimeSpan.FromSeconds(0.25))
        //   .Subscribe(_ =>
        //   {
        //       foreach (var (index, vm) in Subscriptions.Select((x, i) => (i, x)))
        //       {
        //           var subscEntity = vm._source;
        //           subscEntity.SortIndex = index + 1; // 新規追加時に既存アイテムを後ろにずらして表示したいため+1
        //           _subscriptionManager.UpdateSubscription(subscEntity);
        //       }
        //   })
        //   .AddTo(_CompositeDisposable);

        IsAutoUpdateRunning = _subscriptionUpdateManager.ObserveProperty(x => x.IsRunning)
            .ToReadOnlyReactiveProperty(false)
            .AddTo(_CompositeDisposable);
        AutoUpdateFrequency = _subscriptionUpdateManager.ToReactivePropertyAsSynchronized(x => x.UpdateFrequency)
            .AddTo(_CompositeDisposable);
        IsAutoUpdateEnabled = _subscriptionUpdateManager.ToReactivePropertyAsSynchronized(x => x.IsAutoUpdateEnabled)
            .AddTo(_CompositeDisposable);
    }

    

    public override void Dispose()
    {
        base.Dispose();
    }

    SubscriptionGroupViewModel ToSubscriptionGroupVM(SubscriptionGroup subscriptionGroup)
    {
        return new SubscriptionGroupViewModel(this, subscriptionGroup, _subscriptionManager, _queuePlaylist, _scheduler, _logger, _messenger, _dialogService, _notificationService);
    }

    void ClearSubscriptionGroupVM()
    {
        foreach (var groupVM in SubscriptionGroups)
        {
            (groupVM as IDisposable)?.Dispose();
        }
        SubscriptionGroups.Clear();
    }

    void ResetSubscriptionGroups()
    {
        ClearSubscriptionGroupVM();
        SubscriptionGroups.Add(ToSubscriptionGroupVM(_subscriptionManager.DefaultSubscriptionGroup));
        foreach (var subscGroup in _subscriptionManager.GetSubscriptionGroups())
        {
            SubscriptionGroups.Add(ToSubscriptionGroupVM(subscGroup));
        }
    }

    public override void OnNavigatingTo(INavigationParameters parameters)
    {
        ResetSubscriptionGroups();

        _messenger.Register<SubscriptionGroupDeletedMessage>(this, (r, m) =>
        {
            var defaultGroup = SubscriptionGroups[0];
            var groupVM = SubscriptionGroups.FirstOrDefault(x => x.SubscriptionGroup.GroupId == m.Value.GroupId);
            if (groupVM != null)
            {
                // 削除後はデフォルトグループに移動させる
                foreach (var subscVM in groupVM.Subscriptions)
                {
                    defaultGroup.Subscriptions.Add(subscVM);
                }

                SubscriptionGroups.Remove(groupVM);                
            }
        });

        base.OnNavigatingTo(parameters);
    }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        _messenger.Unregister<SubscriptionGroupDeletedMessage>(this);

        base.OnNavigatedFrom(parameters);
    }

    CancellationTokenSource _cancellationTokenSource;
    
    [RelayCommand]
    void AllUpdate()
    {
        _scheduler.Schedule(async () =>
        {
            using (_cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
            {
                try
                {
                    await _subscriptionUpdateManager.UpdateIfOverExpirationAsync(_cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    
                }
            }
        });

        //Analytics.TrackEvent("Subscription_Update");
    }
  
    [RelayCommand]
    void CancelUpdate()
    {
        _cancellationTokenSource?.Cancel();
    }

    public void OpenSourceVideoListPage(object sender, ItemClickEventArgs args)
    {
        var source = (args.ClickedItem as SubscriptionViewModel)!._source;
        (HohoemaPageType pageType, string param) = source.SourceType switch
        {
            SubscriptionSourceType.Mylist => (HohoemaPageType.Mylist, $"id={source.SourceParameter}"),
            SubscriptionSourceType.User => (HohoemaPageType.UserVideo, $"id={source.SourceParameter}"),
            SubscriptionSourceType.Channel => (HohoemaPageType.ChannelVideo, $"id={source.SourceParameter}"),
            SubscriptionSourceType.Series => (HohoemaPageType.Series, $"id={source.SourceParameter}"),
            SubscriptionSourceType.SearchWithKeyword => (HohoemaPageType.SearchResultKeyword, $"keyword={Uri.EscapeDataString(source.SourceParameter)}"),
            SubscriptionSourceType.SearchWithTag => (HohoemaPageType.SearchResultTag, $"keyword={Uri.EscapeDataString(source.SourceParameter)}"),
            _ => throw new NotImplementedException(),
        };

        _ = _messenger.SendNavigationRequestAsync(pageType, param);
    }



    [RelayCommand]
    void OpenSubscVideoListPage()
    {
        _ = _messenger.SendNavigationRequestAsync(HohoemaPageType.SubscVideoList);
    }

    [RelayCommand]
    async Task AddSubscriptionGroup(SubscriptionViewModel subscVM)
    {
        var name =await _dialogService.GetTextAsync(
            "AddSubscriptionGroup_InputSubscGroupName_Title".Translate(),
            "",
            "",
            (s) => !string.IsNullOrWhiteSpace(s)
            );

        if (string.IsNullOrWhiteSpace(name)) { return; }

        SubscriptionGroup newGroup = _subscriptionManager.CreateSubscriptionGroup(name);
        SubscriptionGroups.Add(ToSubscriptionGroupVM(newGroup));

        subscVM.ChangeSubscGroup(newGroup);
    }
}

public sealed partial class SubscriptionGroupViewModel 
    : ObservableObject
    , IRecipient<SubscriptionAddedMessage>
    , IRecipient<SubscriptionDeletedMessage>
    , IRecipient<SubscriptionGroupMovedMessage>
    , IDisposable
{
    public ObservableCollection<SubscriptionViewModel> Subscriptions { get; }
    public SubscriptionGroup SubscriptionGroup { get; }

    private readonly SubscriptionManagementPageViewModel _pageVM;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly QueuePlaylist _queuePlaylist;
    private readonly IScheduler _scheduler;
    private readonly ILogger _logger;
    private readonly IMessenger _messenger;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly SubscriptionGroupProps _groupProps;


    [ObservableProperty]
    private bool _hasNewVideos;

    [ObservableProperty]
    private int _unwatchedVideoCount;

    partial void OnUnwatchedVideoCountChanged(int value)
    {
        HasNewVideos = value != 0;
    }

    [ObservableProperty]
    private bool _isAutoUpdateEnabled;

    partial void OnIsAutoUpdateEnabledChanged(bool value)
    {
        _groupProps.IsAutoUpdateEnabled = value;
        _subscriptionManager.SetSubcriptionGroupProps(_groupProps);

        SetChildSubscriptionAutoUpdate();

        Debug.WriteLine($"{SubscriptionGroup.Name} SubscGroup IsAutoUpdateEnabled : {value}");
    }


    [ObservableProperty]
    private bool _isToastNotificationEnabled;

    partial void OnIsToastNotificationEnabledChanged(bool value)
    {
        _groupProps.IsToastNotificationEnabled = value;

        _subscriptionManager.SetSubcriptionGroupProps(_groupProps);
        Debug.WriteLine($"{SubscriptionGroup.Name} SubscGroup IsToastNotificationEnabled : {value}");
    }

    [ObservableProperty]
    private bool _isInAppLiteNotificationEnabled;

    partial void OnIsInAppLiteNotificationEnabledChanged(bool value)
    {
        _groupProps.IsInAppLiteNotificationEnabled = value;

        _subscriptionManager.SetSubcriptionGroupProps(_groupProps);
        Debug.WriteLine($"{SubscriptionGroup.Name} SubscGroup IsInAppLiteNotificationEnabled : {value}");
    }

    [ObservableProperty]
    private bool _isShowInAppMenu;

    partial void OnIsShowInAppMenuChanged(bool value)
    {
        _groupProps.IsShowInAppMenu = value;

        _subscriptionManager.SetSubcriptionGroupProps(_groupProps);
        Debug.WriteLine($"{SubscriptionGroup.Name} SubscGroup IsShowInAppMenu : {value}");
    }

    [ObservableProperty]
    private bool _isAddToQueueWhenUpdated;

    partial void OnIsAddToQueueWhenUpdatedChanged(bool value)
    {
        _groupProps.IsAddToQueueWhenUpdated = value;

        _subscriptionManager.SetSubcriptionGroupProps(_groupProps);
        Debug.WriteLine($"{SubscriptionGroup.Name} SubscGroup IsAddToQueueWhenUpdated : {value}");
    }

    public SubscriptionGroupViewModel(
        SubscriptionManagementPageViewModel pageVM,
        SubscriptionGroup subscriptionGroup,
        SubscriptionManager subscriptionManager,
        QueuePlaylist queuePlaylist,
        IScheduler scheduler,
        ILogger logger,
        IMessenger messenger,
        IDialogService dialogService,
        INotificationService notificationService
        )
    {
        _pageVM = pageVM;
        SubscriptionGroup = subscriptionGroup;
        _subscriptionManager = subscriptionManager;
        _queuePlaylist = queuePlaylist;
        _scheduler = scheduler;
        _logger = logger;
        _messenger = messenger;
        _dialogService = dialogService;
        _notificationService = notificationService;
        Subscriptions = new ObservableCollection<SubscriptionViewModel>(
            _subscriptionManager.GetSubscriptions(SubscriptionGroup.GroupId)
            .Select(ToSubscriptionVM)
            );

        _messenger.Register<SubscriptionAddedMessage>(this);
        _messenger.Register<SubscriptionDeletedMessage>(this);
        _messenger.Register<SubscriptionGroupMovedMessage>(this);

        _groupProps = _subscriptionManager.GetSubscriptionGroupProps(SubscriptionGroup.GroupId);
        _unwatchedVideoCount = _subscriptionManager.GetFeedVideosCountWithNewer(SubscriptionGroup.GroupId);
        _hasNewVideos = _unwatchedVideoCount != 0;
        _isToastNotificationEnabled = _groupProps.IsToastNotificationEnabled;
        _isInAppLiteNotificationEnabled = _groupProps.IsInAppLiteNotificationEnabled;
        _isShowInAppMenu = _groupProps.IsShowInAppMenu;
        _isAddToQueueWhenUpdated = _groupProps.IsAddToQueueWhenUpdated;
        _isAutoUpdateEnabled = _groupProps.IsAutoUpdateEnabled;
        SetChildSubscriptionAutoUpdate();
    }

    private void SetChildSubscriptionAutoUpdate()
    {
        foreach (var subscVM in Subscriptions)
        {
            subscVM.IsParentGroupAutoUpdateEnabled = _isAutoUpdateEnabled;
        }
    }

    private SubscriptionViewModel ToSubscriptionVM(Models.Subscriptions.Subscription subscription)
    {
        return new SubscriptionViewModel(subscription, _subscriptionManager, this, _queuePlaylist, _logger, _messenger, _dialogService, _notificationService);
    }

    void IRecipient<SubscriptionAddedMessage>.Receive(SubscriptionAddedMessage m)
    {
        _scheduler.Schedule(() =>
        {
            if (m.Value.Group == null)
            {
                if (!SubscriptionGroup.IsDefaultGroup)
                {
                    return;
                } 
            }
            else
            {
                if (SubscriptionGroup.GroupId != m.Value.Group.GroupId)
                {
                    return;
                }
            }

            Subscriptions.Add(ToSubscriptionVM(m.Value));
        });
    }

    void IRecipient<SubscriptionDeletedMessage>.Receive(SubscriptionDeletedMessage m)
    {
        _scheduler.Schedule(() =>
        {
            var target = Subscriptions.FirstOrDefault(x => x._source.SubscriptionId == m.Value);
            if (target == null) { return; }
            Subscriptions.Remove(target);
        });
    }

    void IRecipient<SubscriptionGroupMovedMessage>.Receive(SubscriptionGroupMovedMessage message)
    {
        _scheduler.Schedule(() =>
        {
            if (SubscriptionGroup.GroupId == message.CurrentGroupId)
            {
                if (Subscriptions.Any(x => x._source.SubscriptionId == message.Value.SubscriptionId))
                {
                    return;
                }

                Subscriptions.Add(ToSubscriptionVM(message.Value));
            }
            else if (SubscriptionGroup.GroupId == message.LastGroupId)
            {
                var removed = Subscriptions.FirstOrDefault(x => x._source.SubscriptionId == message.Value.SubscriptionId);
                if (removed != null)
                {
                    Subscriptions.Remove(removed);
                }                    
            }
        });
    }

    public void Dispose()
    {
        _messenger.Unregister<SubscriptionAddedMessage>(this);
        _messenger.Unregister<SubscriptionDeletedMessage>(this);
        _messenger.Unregister<SubscriptionGroupMovedMessage>(this);

        foreach (var subscriptionVM in Subscriptions)
        {
            (subscriptionVM as IDisposable)?.Dispose();
        }
    }

    [RelayCommand]
    async Task PlayNewVideo()
    {
        var video = _subscriptionManager.GetSubscFeedVideosNewerAt(SubscriptionGroup.GroupId, limit: 1).FirstOrDefault();
        if (video != null)
        {
            await _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(SubscriptionGroup.GroupId.ToString(), PlaylistItemsSourceOrigin.SubscriptionGroup, string.Empty, video.VideoId));
        }
    }

    [RelayCommand]
    void AddToQueue()
    {
        var videos = _subscriptionManager.GetSubscFeedVideosNewerAt(SubscriptionGroup.GroupId);
        int prevVount = _queuePlaylist.Count;
        foreach (var video in videos)
        {
            _queuePlaylist.Add(video);
        }

        var subCount = _queuePlaylist.Count - prevVount;
        if (0 < subCount)
        {
            _notificationService.ShowLiteInAppNotification("Notification_SuccessAddToWatchLaterWithAddedCount".Translate(subCount));
        }
    }

    [RelayCommand]
    async Task OpenSubscVideoListPage()
    {
        await _messenger.SendNavigationRequestAsync(HohoemaPageType.SubscVideoList, 
            new NavigationParameters( ("SubscGroupId", SubscriptionGroup.GroupId.ToString())) 
            );
    }

    [RelayCommand]
    async Task RenameSubscriptionGroup()
    {
        var group = SubscriptionGroup;
        string? resultName = await _dialogService.GetTextAsync("SubscGroup_Rename".Translate(), "", group.Name, (s) => !string.IsNullOrWhiteSpace(s) && s.Length <= 40);
        if (resultName is null) { return; }

        group.Name = resultName;
        _subscriptionManager.UpdateSubscriptionGroup(group);

        foreach (var subsc in Subscriptions)
        {
            if (subsc.Group?.GroupId == group.GroupId)
            {
                subsc.Group = null;
                subsc.Group = group;
            }
        }
    }


    [RelayCommand]
    async Task DeleteSubscriptionGroup()
    {
        var group = SubscriptionGroup;
        bool confirmDelete = await _dialogService.ShowMessageDialog(
            "SubscGroup_DeleteComfirmDialogContent".Translate(),
            "SubscGroup_DeleteComfirmDialogTitle".Translate(group.Name),
            "Delete".Translate(),
            "Cancel".Translate()
            );

        if (confirmDelete)
        {
            _subscriptionManager.DeleteSubscriptionGroup(group);
            /*
            foreach (var subsc in Subscriptions)
            {
                if (subsc.Group?.GroupId == group.GroupId)
                {
                    subsc.Group = null;
                }
            }
            */
        }
    }



    void SaveOrder()
    {
        _subscriptionManager.ReoderSubscriptionGroups(_pageVM.SubscriptionGroups.Select(x => x.SubscriptionGroup));
    }

    [RelayCommand]
    void MoveToPreview()
    {        
        var index = _pageVM.SubscriptionGroups.IndexOf(this);
        if (index - 1 >= 1) //  デフォルトグループは常に先頭として扱う
        {
            _pageVM.SubscriptionGroups.Remove(this);
            _pageVM.SubscriptionGroups.Insert(index - 1, this);
        }

        SaveOrder();
    }

    [RelayCommand]
    void MoveToNext()
    {
        var index = _pageVM.SubscriptionGroups.IndexOf(this);
        if (index + 1 < _pageVM.SubscriptionGroups.Count)
        {
            _pageVM.SubscriptionGroups.Remove(this);
            _pageVM.SubscriptionGroups.Insert(index + 1, this);
        }

        SaveOrder();
    }

    [RelayCommand]
    void MoveToHead()
    {
        if (this == _pageVM.SubscriptionGroups.FirstOrDefault()) { return; }
        _pageVM.SubscriptionGroups.Remove(this);
        _pageVM.SubscriptionGroups.Insert(1, this); //  デフォルトグループは常に先頭として扱う

        SaveOrder();
    }

    [RelayCommand]
    void MoveToTail()
    {
        if (this == _pageVM.SubscriptionGroups.LastOrDefault()) { return; }

        _pageVM.SubscriptionGroups.Remove(this);
        _pageVM.SubscriptionGroups.Add(this);

        SaveOrder();
    }

}


public partial class SubscriptionViewModel 
    : ObservableObject
    , IDisposable
    , IRecipient<SubscriptionFeedUpdatedMessage>
{

    internal readonly Models.Subscriptions.Subscription _source;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly SubscriptionGroupViewModel _pageViewModel;
    private readonly QueuePlaylist _queuePlaylist;
    private readonly ILogger _logger;
    private readonly IMessenger _messenger;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;

    public string SourceParameter => _source.SourceParameter;
    public SubscriptionSourceType SourceType => _source.SourceType;
    public string Label => _source.Label;

    [ObservableProperty]
    private bool _isAutoUpdateEnabled;

    partial void OnIsAutoUpdateEnabledChanged(bool value)
    {
        _source.IsAutoUpdateEnabled = value;
        _subscriptionManager.UpdateSubscription(_source);
        CombinedAutoUpdateEnabledSubscAndGroup = _isAutoUpdateEnabled && _isParentGroupAutoUpdateEnabled;

        Debug.WriteLine($"{_source.Label} subsc IsEnabled : {value}");
    }

    [ObservableProperty]
    private IVideoContent? _sampleVideo;

    [ObservableProperty]
    private DateTime _lastUpdatedAt;

    [ObservableProperty]
    private DateTime _nextUpdateAt;

    [ObservableProperty]
    private bool _nowUpdating;

    [ObservableProperty]
    private int _unwatchedVideoCount;

    partial void OnUnwatchedVideoCountChanged(int value)
    {
        HasNewVideos = value != 0;
    }

    [ObservableProperty]
    private bool _isParentGroupAutoUpdateEnabled;

    [ObservableProperty]
    private bool _hasNewVideos;

    partial void OnIsParentGroupAutoUpdateEnabledChanged(bool value)
    {
        CombinedAutoUpdateEnabledSubscAndGroup = _isAutoUpdateEnabled && _isParentGroupAutoUpdateEnabled;
    }

    [ObservableProperty]
    private bool _combinedAutoUpdateEnabledSubscAndGroup;



    [ObservableProperty]
    private bool _isToastNotificationEnabled;

    partial void OnIsToastNotificationEnabledChanged(bool value)
    {
        _source.IsToastNotificationEnabled = value;

        _subscriptionManager.UpdateSubscription(_source);
        Debug.WriteLine($"{_source.Label} SubscGroup IsToastNotificationEnabled : {value}");
    }

    [ObservableProperty]
    private bool _isAddToQueueWhenUpdated;

    partial void OnIsAddToQueueWhenUpdatedChanged(bool value)
    {
        _source.IsAddToQueueWhenUpdated = value;

        _subscriptionManager.UpdateSubscription(_source);
        Debug.WriteLine($"{_source.Label} SubscGroup IsAddToQueueWhenUpdated : {value}");
    }

    public SubscriptionViewModel(
        Models.Subscriptions.Subscription source,
        SubscriptionManager subscriptionManager,
        SubscriptionGroupViewModel pageViewModel,
        QueuePlaylist queuePlaylist,
        ILogger logger,
        IMessenger messenger,
        IDialogService dialogService,
        INotificationService notificationService
        )
    {
        _logger = logger;
        _messenger = messenger;
        _source = source;
        _pageViewModel = pageViewModel;
        _queuePlaylist = queuePlaylist;
        _subscriptionManager = subscriptionManager;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _isAutoUpdateEnabled = _source.IsAutoUpdateEnabled;
        _group = _source.Group;

        var lastUpdatedAt = _subscriptionManager.GetLastUpdatedAt(_source.SubscriptionId);
        _lastUpdatedAt = lastUpdatedAt;
        _nextUpdateAt = _subscriptionManager.GetNextUpdateTime(lastUpdatedAt);
        _unwatchedVideoCount = _subscriptionManager.GetFeedVideosCountWithNewer(_source);
        _hasNewVideos = _unwatchedVideoCount != 0;
        _isToastNotificationEnabled = _source.IsToastNotificationEnabled;
        _isAddToQueueWhenUpdated = _source.IsAddToQueueWhenUpdated;

        // Note: GroupVM側で IsParentGroupAutoUpdateEnabled を初期設定されるのに任せる
        // コメントアウトしたままにしておく
        //_combinedAutoUpdateEnabledSubscAndGroup = _isAutoUpdateEnabled && _isParentGroupAutoUpdateEnabled;

        _messenger.Register<SubscriptionFeedUpdatedMessage>(this);
    }

    void IDisposable.Dispose()
    {
        _messenger.Unregister<SubscriptionFeedUpdatedMessage>(this);
    }

    void IRecipient<SubscriptionFeedUpdatedMessage>.Receive(SubscriptionFeedUpdatedMessage message)
    {
        UpdateFeedResult(message.Value.NewVideos ?? new List<NicoVideo>(), message.Value.UpdateAt!);
    }



    internal void UpdateFeedResult(IList<NicoVideo> result, DateTime updatedAt)
    {
        _sampleVideo = null;
        if (result.FirstOrDefault() is not null and var video)
        {
            _sampleVideo = video;
        }

        LastUpdatedAt = updatedAt;
        NextUpdateAt = _subscriptionManager.GetNextUpdateTime(updatedAt);
        UnwatchedVideoCount = _subscriptionManager.GetFeedVideosCountWithNewer(_source);
    }
    
    [RelayCommand]
    async Task Update()
    {
        try
        {
            NowUpdating = true;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                var update = _subscriptionManager.GetSubscriptionProps(_source.SubscriptionId);
                if (_subscriptionManager.CheckCanUpdate(isManualUpdate: true, subscription: _source, ref update) is not null and SubscriptionFeedUpdateFailedReason failedReason)
                {                    
                    return;
                }

                var result = await _subscriptionManager.UpdateSubscriptionFeedVideosAsync(_source, update: update, cancellationToken: cts.Token);
                if (result.IsSuccessed)
                {
                    LastUpdatedAt = result.UpdateAt;
                    UnwatchedVideoCount = _subscriptionManager.GetFeedVideosCountWithNewer(_source);
                }
                else
                {
                    UnwatchedVideoCount = 0;
                }
                
            }
        }
        catch (Exception e)
        {
            _logger.ZLogErrorWithPayload(e, _source, "Subscription update failed");
        }
        finally
        {
            NowUpdating = false;
        }
    }

    [RelayCommand]
    async Task OpenSourceVideoListPage()
    {
        (HohoemaPageType pageType, string param) = _source.SourceType switch
        {
            SubscriptionSourceType.Mylist => (HohoemaPageType.Mylist, $"id={_source.SourceParameter}"),
            SubscriptionSourceType.User => (HohoemaPageType.UserVideo, $"id={_source.SourceParameter}"),
            SubscriptionSourceType.Channel => (HohoemaPageType.ChannelVideo, $"id={_source.SourceParameter}"),
            SubscriptionSourceType.Series => (HohoemaPageType.Series, $"id={_source.SourceParameter}"),
            SubscriptionSourceType.SearchWithKeyword => (HohoemaPageType.SearchResultKeyword, $"keyword={Uri.EscapeDataString(_source.SourceParameter)}"),
            SubscriptionSourceType.SearchWithTag => (HohoemaPageType.SearchResultTag, $"keyword={Uri.EscapeDataString(_source.SourceParameter)}"),
            _ => throw new NotImplementedException(),
        };

        await _messenger.SendNavigationRequestAsync(pageType, new NavigationParameters(param));
    }


    [RelayCommand]
    async Task DeleteSubscription()
    {
        var result = await _dialogService.ShowMessageDialog(
            _source.Label,
            "StopSubscribe?".Translate(),
            "StopSubscribe".Translate(),
            "Cancel".Translate()
            );
        if (result)
        {
            _subscriptionManager.RemoveSubscription(_source);
        }
    }


    [ObservableProperty]
    private SubscriptionGroup? _group;

    [RelayCommand]
    public void ChangeSubscGroup(SubscriptionGroup group)
    {
        _subscriptionManager.MoveSubscriptionGroupAndInsertToLast(_source, group);
        Group = group;
    }



    [RelayCommand]
    async Task PlayVideoItem()
    {
        // 最新動画項目の末尾を取得して、再生する
        if (!(_subscriptionManager.GetSubscFeedVideosNewerAt(LastUpdatedAt, 0, 1).FirstOrDefault() is not null and var video))
        {
            return;
        }

#if DEBUG
        Debug.WriteLine($"チェック日時: {LastUpdatedAt:t} 動画投稿日時: {video.PostAt:t}");
#endif
       
        await _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(_source.SubscriptionId.ToString(), PlaylistItemsSourceOrigin.Subscription, string.Empty, video.VideoId));
    }


    [RelayCommand]
    void AddToQueue()
    {
        var videos = _subscriptionManager.GetSubscFeedVideosNewerAt(_source);
        int prevVount = _queuePlaylist.Count;
        foreach (var video in videos)
        {
            _queuePlaylist.Add(video);
        }

        var subCount = _queuePlaylist.Count - prevVount;
        if (0 < subCount)
        {
            _notificationService.ShowLiteInAppNotification("Notification_SuccessAddToWatchLaterWithAddedCount".Translate(subCount));
        }
    }


    [RelayCommand]
    void MoveToPreview()
    {
        var index = _pageViewModel.Subscriptions.IndexOf(this);
        if (index - 1 >= 0)
        {
            _pageViewModel.Subscriptions.Remove(this);
            _pageViewModel.Subscriptions.Insert(index - 1, this);
        }
    }

    [RelayCommand]
    void MoveToNext()
    {
        var index = _pageViewModel.Subscriptions.IndexOf(this);
        if (index + 1 < _pageViewModel.Subscriptions.Count)
        {
            _pageViewModel.Subscriptions.Remove(this);
            _pageViewModel.Subscriptions.Insert(index + 1, this);
        }
    }

    [RelayCommand]
    void MoveToHead()
    {
        if (this == _pageViewModel.Subscriptions.FirstOrDefault()) { return; }
        _pageViewModel.Subscriptions.Remove(this);
        _pageViewModel.Subscriptions.Insert(0, this);
    }

    [RelayCommand]
    void MoveToTail()
    {
        if (this == _pageViewModel.Subscriptions.LastOrDefault()) { return; }

        _pageViewModel.Subscriptions.Remove(this);
        _pageViewModel.Subscriptions.Add(this);
    }

}
