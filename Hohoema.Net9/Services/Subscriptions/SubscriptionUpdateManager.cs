#nullable enable
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Contracts.AppLifecycle;
using Hohoema.Contracts.Navigations;
using Hohoema.Contracts.Services;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Helpers;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
using Hohoema.Models.VideoCache;
using I18NPortable;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.System;
using ZLogger;

namespace Hohoema.Services.Subscriptions;

public readonly struct SubscriptionUpdatedEventArgs 
{
    public SubscriptionUpdatedEventArgs(DateTime updatedTime, DateTime nextUpdateTime)
    {
        UpdatedTime = updatedTime;
        NextUpdateTime = nextUpdateTime;
    }

    public readonly DateTime UpdatedTime;
    public readonly DateTime NextUpdateTime;
}

public class SubscriptionUpdateCompletedMessage : ValueChangedMessage<SubscriptionUpdatedEventArgs>
{
    public SubscriptionUpdateCompletedMessage(DateTime updatedTime, DateTime nextUpdateTime) : base(new (updatedTime, nextUpdateTime))
    {
    }
}


public enum SubscriptionUpdateStatus
{
    NoProbrem,    
    FailedWithOffline,
    FailedWithApiError,
    FailedWithTimeout
}

public sealed partial class SubscriptionUpdateManager 
    : ObservableObject
    , IDisposable
    , IToastActivationAware
    , ISuspendAndResumeAware
{
    private readonly ILogger<SubscriptionUpdateManager> _logger;
    private readonly IMessenger _messenger;
    private readonly INotificationService _notificationService;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly SubscriptionSettings _subscriptionSettings;
    private readonly QueuePlaylist _queuePlaylist;
    private readonly AsyncLock _timerLock = new AsyncLock();

    private bool _isDisposed;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _timer;


    private bool _IsAutoUpdateEnabled;
    public bool IsAutoUpdateEnabled
    {
        get { return _IsAutoUpdateEnabled; }
        private set 
        {
            if (SetProperty(ref _IsAutoUpdateEnabled, value))
            {
                _subscriptionSettings.IsSubscriptionAutoUpdateEnabled = value;

                if (_IsAutoUpdateEnabled)
                {
                    StartOrResetTimer();
                }
                else
                {
                    _ = StopTimerAsync();
                }
            }
        }
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get { return _isRunning; }
        private set { SetProperty(ref _isRunning, value); }
    }

    [ObservableProperty]
    public partial DateTime NextUpdateAt { get; set; }

    [ObservableProperty]
    public partial DateTime LastCheckedAt { get; set; }

    partial void OnLastCheckedAtChanged(DateTime value)
    {
        _subscriptionSettings.SubscriptionsLastUpdatedAt = value;
    }

    public SubscriptionUpdateManager(
        ILoggerFactory loggerFactory,
        IMessenger messenger,
        INotificationService notificationService,
        SubscriptionManager subscriptionManager,
        SubscriptionSettings subscriptionSettingsRepository,
        QueuePlaylist queuePlaylist
        )
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _timer = _dispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMinutes(1);
        _timer.IsRepeating = true;
        _timer.Tick += async (s, e) => 
        {
            await UpdateIfOverExpirationAsync(CancellationToken.None);
        };
        _logger = loggerFactory.CreateLogger<SubscriptionUpdateManager>();
        _messenger = messenger;
        _notificationService = notificationService;
        _subscriptionManager = subscriptionManager;
        _subscriptionSettings = subscriptionSettingsRepository;
        _queuePlaylist = queuePlaylist;
        _messenger.Register<SubscriptionAddedMessage>(this, async (r, m) => 
        {
            using (await _timerLock.LockAsync())
            {
                if (Helpers.InternetConnection.IsInternet() is false) { return; }

                using (_timerUpdateCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    var result = await _subscriptionManager.UpdateSubscriptionFeedVideosAsync(m.Value, cancellationToken: _timerUpdateCancellationTokenSource.Token);
                }

                _timerUpdateCancellationTokenSource = null;
            }
        });

        _IsAutoUpdateEnabled = _subscriptionSettings.IsSubscriptionAutoUpdateEnabled;
        LastCheckedAt = _subscriptionSettings.SubscriptionsLastUpdatedAt;
        NextUpdateAt = _subscriptionManager.GetNextUpdateTime(LastCheckedAt);
        StartOrResetTimer();
    }


    async ValueTask ISuspendAndResumeAware.OnSuspendingAsync()
    {
        try
        {
            _timerUpdateCancellationTokenSource?.Cancel();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, "subscription timer cancel faield.");
        }


        try
        {
            await StopTimerAsync();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, "subscription timer stop faield.");
        }
        finally
        {
        }
    }

    async ValueTask ISuspendAndResumeAware.OnResumingAsync()
    {
        try
        {
            // リジューム復帰直後だと
            await Task.Delay(TimeSpan.FromSeconds(3));

            StartOrResetTimer();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, "購読の定期更新の開始に失敗");
        }
    }

    static readonly TimeSpan UpdateTimeout = TimeSpan.FromMinutes(15);

    public async Task UpdateIfOverExpirationAsync(CancellationToken ct)
    {
        if (Helpers.InternetConnection.IsInternet() is false) 
        {
            return; 
        }

        DateTime checkedAt = DateTime.Now;
        if (checkedAt < NextUpdateAt)
        {            
            return;
        }        

        List<SubscriptionFeedUpdateResult> updateResultItems = new();
        using (_logger.BeginScope("Subscription Update"))
        using (await _timerLock.LockAsync())
        {
            try
            {
                _logger.ZLogDebug("Start.");
                if (_timerUpdateCancellationTokenSource != null)
                {
                    _timerUpdateCancellationTokenSource.Cancel();
                    _timerUpdateCancellationTokenSource.Dispose();
                }
                _timerUpdateCancellationTokenSource = new CancellationTokenSource(UpdateTimeout);
                var timeCt = _timerUpdateCancellationTokenSource.Token;
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeCt))                
                {
                    CancellationToken linkedCt = linkedCts.Token;
                    foreach (var subscription in _subscriptionManager.GetSortedSubscriptions())
                    {
                        if (updateResultItems.Count != 0)
                        {
                            // 常に一秒空ける
                            await Task.Delay(1000, linkedCt);
                        }
                        
                        _logger.ZLogDebug("Start. Label: {0}", subscription.Label);
                        try
                        {
                            var update = _subscriptionManager.GetSubscriptionProps(subscription.SubscriptionId);
                            if (_subscriptionManager.CheckCanUpdate(isManualUpdate: false, subscription, ref update) is not null and SubscriptionFeedUpdateFailedReason failedReason)
                            {
                                _logger.ZLogDebug("Skiped. Label: {0}, Reason: {1}", subscription.Label, failedReason);
                                continue;
                            }
                            var list = await _subscriptionManager.UpdateSubscriptionFeedVideosAsync(subscription, update: update, checkedAt, linkedCt);
                            updateResultItems.Add(list);

                            _logger.ZLogDebug("Done. Label: {0}", subscription.Label);                            
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex) when (InternetConnection.IsInternet())
                        {
                            // インターネット接続はあるけど何かしらで失敗した場合は
                            // API不通を想定して強制的に自動更新をOFFにする
                            subscription.IsAutoUpdateEnabled = false;                            
                            _subscriptionManager.UpdateSubscription(subscription);

                            _messenger.Send(new SubscriptionUpdatedMessage(subscription));
                            _logger.ZLogDebug("Error. Label: {0}, Exception: {1}", subscription.Label, ex.Message);
                        }
                    }
                }                
            }
            catch (OperationCanceledException)
            {
                _logger.ZLogInformation("Timeout subscription auto update process.");
            }
            finally
            {
                // 次の自動更新周期を延長して設定
                LastCheckedAt = checkedAt;
                NextUpdateAt = _subscriptionManager.GetNextUpdateTime(checkedAt);
                _timerUpdateCancellationTokenSource!.Dispose();
                _timerUpdateCancellationTokenSource = null;
                _logger.ZLogDebug("Complete.");
                _messenger.Send(new SubscriptionUpdateCompletedMessage(checkedAt, NextUpdateAt));
                _notificationService.ShowLiteInAppNotification("SubscNotification_CompleteAutoUpdate".Translate());
            }

            // 購読アイテムがあり、かつ新着アイテムが無い場合の通知
            if (updateResultItems.Count != 0 && updateResultItems.Any(x => x.NewVideos.Any()) is false)
            {
                _notificationService.ShowLiteInAppNotification("SubscNotification_NoNewVideos".Translate());
                return;
            }

            try
            {
                AddToQueue(updateResultItems);
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, "failed in new videos add to QueuePlaylist.");
            }

            try
            {
                TriggerToastNotificationFeedUpdateResult(updateResultItems);
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, "failed in new videos notice to user with ToastNotification.");
            }
        }
    }

    public const string ToastArgumentKey_Action = "action";
    public const string ToastArgumentValue_Action_SubscPlay = "SubscPlay";
    public const string ToastArgumentValue_Action_SubscViewList = "SubscViewList";
    public const string ToastArgumentValue_UserInputKey_SubscGroupId = "SelectedSubscGroupId";
    public const string ToastArgumentValue_UserInputValue_NoSelectSubscGroupId = "All";

    static ToastArguments MakeSubscPlayToastArguments()
    {
        ToastArguments args = new()
        {
            { ToastArgumentKey_Action, ToastArgumentValue_Action_SubscPlay },
        };
        return args;
    }

    static ToastArguments MakeSubscViewListToastArguments()
    {
        ToastArguments args = new()
        {
            { ToastArgumentKey_Action, ToastArgumentValue_Action_SubscViewList },
        };
        return args;
    }

    async ValueTask<bool> IToastActivationAware.TryHandleActivationAsync(ToastArguments arguments, ValueSet userInput)
    {
        if (!arguments.TryGetValue(ToastArgumentKey_Action, out string actionType))
        {
            return false;
        }
        bool isHandled = false;
        switch (actionType)
        {                
            case ToastArgumentValue_Action_SubscPlay:
                isHandled = true;
                if (userInput.TryGetValue(ToastArgumentValue_UserInputKey_SubscGroupId, out object groupIdPlayStr)
                    && groupIdPlayStr is string and var groupIdPlay
                    && groupIdPlay != ToastArgumentValue_UserInputValue_NoSelectSubscGroupId
                    )
                {
                    // Note: ObjectId.Empty は デフォルトグループを指す
                    var groupId = SubscriptionGroupId.Parse(groupIdPlay);
                    var recentVideoInUnchecked = _subscriptionManager.GetSubscFeedVideosNewerAt(groupId).FirstOrDefault();
                    Guard.IsNotNull(recentVideoInUnchecked, nameof(recentVideoInUnchecked));
                    await _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(groupId.ToString(), PlaylistItemsSourceOrigin.SubscriptionGroup, string.Empty, recentVideoInUnchecked.VideoId));
                }
                else
                {
                    await _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(_subscriptionManager.AllSubscriptouGroupId, PlaylistItemsSourceOrigin.SubscriptionGroup, string.Empty));
                }
                  
                break;
            case ToastArgumentValue_Action_SubscViewList:
                isHandled = true;
                if (userInput.TryGetValue(ToastArgumentValue_UserInputKey_SubscGroupId, out object groupIdViewListStr)
                    && groupIdViewListStr is string and var groupIdViewList
                    && groupIdViewList != ToastArgumentValue_UserInputValue_NoSelectSubscGroupId
                    )
                {
                    // Note: ObjectId.Empty は デフォルトグループを指す
                    try
                    {
                        ObjectId groupId = new ObjectId(groupIdViewList);
                        await _messenger.OpenPageAsync(HohoemaPageType.SubscVideoList, new NavigationParameters(("SubscGroupId", groupId.ToString())));
                    }
                    catch
                    {
                        // 購読グループが既に消されていた場合など                            
                        await _messenger.OpenPageAsync(HohoemaPageType.SubscVideoList);
                    }
                }
                else
                {
                    await _messenger.OpenPageAsync(HohoemaPageType.SubscVideoList);
                }
                break;
        }
        
        return isHandled;
    }

    private void TriggerToastNotificationFeedUpdateResult(IEnumerable<SubscriptionFeedUpdateResult> updateResults)
    {
        if (!updateResults.Any()) { return; }

        var resultByGroupId = updateResults
            .Where(x => x.IsSuccessed && x.NewVideos?.Count > 0 && x.Subscription.IsToastNotificationEnabled)
            .GroupBy(x => x.Subscription.Group ?? _subscriptionManager.DefaultSubscriptionGroup, SubscriptionGroupComparer.Default)
            .Where(x => _subscriptionManager.GetSubscriptionGroupProps(x.Key.GroupId).IsToastNotificationEnabled)
            .ToArray();

        if (!resultByGroupId.Any()) { return; }

        ToastSelectionBox box = new ToastSelectionBox(ToastArgumentValue_UserInputKey_SubscGroupId)
        {
            DefaultSelectionBoxItemId = ToastArgumentValue_UserInputValue_NoSelectSubscGroupId,
            Title = "SubscriptionGroup".Translate()
        };

        int totalNewVideoCount = resultByGroupId.Where(x => x.Any()).Sum(x => x.Sum(x => x.NewVideos!.Count));
        box.Items.Add(new ToastSelectionBoxItem(ToastArgumentValue_UserInputValue_NoSelectSubscGroupId, $"{"All".Translate()} - {"VideosWithCount".Translate(totalNewVideoCount)}"));
        foreach (var group in resultByGroupId)
        {        
            box.Items.Add(new ToastSelectionBoxItem(group.Key.GroupId.ToString(), $"{group.Key.Name} - {"VideosWithCount".Translate(group.Sum(x => x.NewVideos!.Count))}"));
        }

        var newVideoOwnersText = "SubscNotification_SampleVideosHeader".Translate();
        string contentText = string.Join("\n", resultByGroupId.SelectMany(x => x.Select(x => $"- " + x.NewVideos.First().Title)).Take(3));
        _notificationService.ShowToast(
            newVideoOwnersText,
            //string.Join("\n", resultByGroupId.Where(x => x.Any()).Select(x => $"{x.Key.Name} - {x.Sum(x => x.NewVideos!.Count)}件")),
            contentText,
            Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Short,
            //luanchContent: PlayWithWatchAfterPlaylistParam,
            toastButtons: new IToastButton[] {
                new ToastButton("SubscGroup_AllPlayUnwatched".Translate(), MakeSubscPlayToastArguments().ToString()),
                new ToastButton("OpenSubscriptionSourceVideoList".Translate(), MakeSubscViewListToastArguments().ToString()),
            },
            toastInputs: new IToastInput[] {
                box
            }
            );
    }

    private void AddToQueue(IEnumerable<SubscriptionFeedUpdateResult> resultItems)
    {
        if (!resultItems.Any()) 
        {            
            return; 
        }

        var resultByGroupId = resultItems
            .Where(x => x.IsSuccessed && x.NewVideos?.Count > 0 && x.Subscription.IsAddToQueueWhenUpdated)
            .GroupBy(x => x.Subscription.Group ?? _subscriptionManager.DefaultSubscriptionGroup, SubscriptionGroupComparer.Default)
            .Where(x => _subscriptionManager.GetSubscriptionGroupProps(x.Key.GroupId).IsAddToQueueWhenUpdated)
            .ToArray();

        if (!resultByGroupId.Any()) { return; }

        int count = 0;
        foreach (var item in resultByGroupId.SelectMany(x => x.SelectMany(x => x.NewVideos)))
        {
            _queuePlaylist.Add(item);
            count++;
        }

        _notificationService.ShowLiteInAppNotification("Notification_SuccessAddToWatchLaterWithAddedCount".Translate(count));
    }


#if DEBUG
    public void TestNotification()
    {
        List<SubscriptionFeedUpdateResult> results = new();
        var sources = _subscriptionManager.GetSubscriptionsWithoutSort().Take(3);
        var updateAt = DateTime.Now;
        foreach (var source in sources)
        {
            var videos = _subscriptionManager.GetSubscFeedVideos(source, 0, 5).Select(x => new NicoVideo() { Id = x.VideoId, Title = x.Title }).ToList();
            results.Add(new SubscriptionFeedUpdateResult(source, videos, updateAt));
        }

        TriggerToastNotificationFeedUpdateResult(results);
    }
#endif

    public void RestartIfTimerNotRunning()
    {
        if (!IsRunning)
        {
            StartOrResetTimer();
        }
    }

    CancellationTokenSource? _timerUpdateCancellationTokenSource;

    async void StartOrResetTimer()
    {
        using (await _timerLock.LockAsync())
        {
            if (_isDisposed) { return; }

            if (!_IsAutoUpdateEnabled) { return; }

            IsRunning = true;            
            _timer.Start();
            _ = UpdateIfOverExpirationAsync(CancellationToken.None);
        }
    }


    async Task StopTimerAsync()
    {
        using (await _timerLock.LockAsync())
        {
            IsRunning = false;
            _timer.Stop();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) { return; }

        _isDisposed = true;
        _timer.Stop();
    }

}
