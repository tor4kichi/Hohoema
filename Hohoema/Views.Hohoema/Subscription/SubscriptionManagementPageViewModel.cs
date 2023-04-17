#nullable enable
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Navigations;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
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
    private readonly IScheduler _scheduler;
    private readonly IMessenger _messenger;
    private readonly ILogger<SubscriptionManagementPageViewModel> _logger;

    public IReadOnlyReactiveProperty<bool> IsAutoUpdateRunning { get; }
    public IReactiveProperty<TimeSpan> AutoUpdateFrequency { get; }
    public IReactiveProperty<bool> IsAutoUpdateEnabled { get; }
    
    public ObservableCollection<SubscriptionGroupViewModel> SubscriptionGroups { get; } = new();
    public ApplicationLayoutManager ApplicationLayoutManager { get; }

    void IRecipient<SettingsRestoredMessage>.Receive(SettingsRestoredMessage message)
    {
        
    }

    public SubscriptionManagementPageViewModel(
        ILoggerFactory loggerFactory,
        IScheduler scheduler, 
        IMessenger messenger,
        SubscriptionManager subscriptionManager,
        SubscriptionUpdateManager subscriptionUpdateManager,
        DialogService dialogService,
        PageManager pageManager,
        ApplicationLayoutManager applicationLayoutManager
        )
    {
        _logger = loggerFactory.CreateLogger<SubscriptionManagementPageViewModel>();
        _scheduler = scheduler;
        _messenger = messenger;
        _subscriptionManager = subscriptionManager;
        _subscriptionUpdateManager = subscriptionUpdateManager;
        _dialogService = dialogService;        
        ApplicationLayoutManager = applicationLayoutManager;
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
        return new SubscriptionGroupViewModel(subscriptionGroup, _subscriptionManager, _scheduler, _logger, _messenger, _dialogService);
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

        //_messenger.Register<SubscriptionUpdatedMessage>(this, (r, m) =>
        //{
        //    var entity = m.Value;
        //    var vm = Subscriptions.FirstOrDefault(x => x.SourceType == entity.SourceType && x.SourceParameter == entity.SourceParameter);
        //    if (vm != null)
        //    {
        //        //_scheduler.Schedule(() =>
        //        //{
        //        //    vm.UpdateFeedResult(e.Videos, DateTime.Now);
        //        //});
        //    }
        //});

        _messenger.Register<SubscriptionGroupDeletedMessage>(this, (r, m) =>
        {
            var groupVM = SubscriptionGroups.FirstOrDefault(x => x.SubscriptionGroup.GroupId == m.Value.GroupId);
            if (groupVM != null)
            {
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

    private readonly SubscriptionManager _subscriptionManager;
    private readonly IScheduler _scheduler;
    private readonly ILogger _logger;
    private readonly IMessenger _messenger;
    private readonly IDialogService _dialogService;
    
    private readonly SubscriptionGroupProps _subscriptionGroupProps;


    [ObservableProperty]
    private bool _isAutoUpdateEnabled;

    partial void OnIsAutoUpdateEnabledChanged(bool value)
    {
        _subscriptionGroupProps.IsAutoUpdateEnabled = value;
        _subscriptionManager.SetSubcriptionGroupProps(_subscriptionGroupProps);

        SetChildSubscriptionAutoUpdate();

        Debug.WriteLine($"{SubscriptionGroup.Name} SubscGroup IsAutoUpdateEnabled : {value}");
    }

    public SubscriptionGroupViewModel(
        SubscriptionGroup subscriptionGroup,
        SubscriptionManager subscriptionManager,
        IScheduler scheduler,
        ILogger logger,
        IMessenger messenger,
        IDialogService dialogService
        )
    {
        SubscriptionGroup = subscriptionGroup;
        _subscriptionManager = subscriptionManager;
        _scheduler = scheduler;
        _logger = logger;
        _messenger = messenger;
        _dialogService = dialogService;
        Subscriptions = new ObservableCollection<SubscriptionViewModel>(
            _subscriptionManager.GetSubscriptions(SubscriptionGroup.GroupId)
            .Select(ToSubscriptionVM)
            );

        _subscriptionGroupProps = _subscriptionManager.GetSubscriptionGroupProps(SubscriptionGroup.GroupId);

        _messenger.Register<SubscriptionAddedMessage>(this);
        _messenger.Register<SubscriptionDeletedMessage>(this);
        _messenger.Register<SubscriptionGroupMovedMessage>(this);

        _isAutoUpdateEnabled = _subscriptionGroupProps.IsAutoUpdateEnabled;
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
        return new SubscriptionViewModel(subscription, _subscriptionManager, this, _logger, _messenger, _dialogService);
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

    
}


public partial class SubscriptionViewModel 
    : ObservableObject
{
    public SubscriptionViewModel(
        Models.Subscriptions.Subscription source,
        SubscriptionManager subscriptionManager,
        SubscriptionGroupViewModel pageViewModel,
        ILogger logger,
        IMessenger messenger,
        IDialogService dialogService
        )
    {
        _logger = logger;
        _messenger = messenger;
        _source = source;
        _pageViewModel = pageViewModel;
        _subscriptionManager = subscriptionManager;
        _dialogService = dialogService;
        _isEnabled = _source.IsAutoUpdateEnabled;
        _group = _source.Group;
    }

    internal readonly Models.Subscriptions.Subscription _source;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly SubscriptionGroupViewModel _pageViewModel;
    private readonly ILogger _logger;
    private readonly IMessenger _messenger;
    private readonly IDialogService _dialogService;

    public string SourceParameter => _source.SourceParameter;
    public SubscriptionSourceType SourceType => _source.SourceType;
    public string Label => _source.Label;

    [ObservableProperty]
    private bool _isEnabled;

    partial void OnIsEnabledChanged(bool value)
    {
        _source.IsAutoUpdateEnabled = value;
        _subscriptionManager.UpdateSubscription(_source);

        Debug.WriteLine($"{_source.Label} subsc IsEnabled : {value}");
    }

    [ObservableProperty]
    private IVideoContent? _sampleVideo;

    [ObservableProperty]
    private DateTime _lastUpdatedAt;

    [ObservableProperty]
    private bool _nowUpdating;

    [ObservableProperty]
    private int _unwatchedVideoCount;

    [ObservableProperty]
    private bool _isParentGroupAutoUpdateEnabled;

    internal void UpdateFeedResult(IList<NicoVideo> result, DateTime updatedAt)
    {
        _sampleVideo = null;
        if (result.FirstOrDefault() is not null and var video)
        {
            _sampleVideo = video;
        }

        LastUpdatedAt = updatedAt;
    }
    
    [RelayCommand]
    async Task Update()
    {
        try
        {
            NowUpdating = true;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                if (_subscriptionManager.CheckCanUpdate(isManualUpdate: true, subscription: _source) is not null and SubscriptionFeedUpdateFailedReason failedReason)
                {                    
                    return;
                }

                var result = await _subscriptionManager.UpdateSubscriptionFeedVideosAsync(_source, cancellationToken: cts.Token);
                if (result.IsSuccessed)
                {
                    UnwatchedVideoCount = result.NewVideos.Count;
                    LastUpdatedAt = result.UpdateAt;
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
        _subscriptionManager.MoveSubscriptionGroup(_source, group);
        Group = group;
    }



    [RelayCommand]
    async Task PlayVideoItem()
    {
        // TODO: 購読ソース選択時の動画再生
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
