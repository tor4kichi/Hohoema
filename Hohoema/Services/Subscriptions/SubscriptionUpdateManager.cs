#nullable enable
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
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

// Note: Subscriptionsが０個の場合、自動更新は必要ないが
// 自動更新周期が長く、また０個であれば処理時間も短く済むため、場合分け処理を入れていない

public class SubscriptionUpdatedEventArgs
{
    public DateTime UpdatedTime { get; set; }
    public DateTime NextUpdateTime { get; set; }
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
    private DateTime _nextUpdateAt;

    [ObservableProperty]
    private DateTime _lastCheckedAt;

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
        _lastCheckedAt = _subscriptionSettings.SubscriptionsLastUpdatedAt;
        _nextUpdateAt = _subscriptionManager.GetNextUpdateTime(_lastCheckedAt);
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

    static readonly TimeSpan UpdateTimeout = TimeSpan.FromMinutes(5);

    public async Task UpdateIfOverExpirationAsync(CancellationToken ct)
    {
        if (Helpers.InternetConnection.IsInternet() is false) 
        {
            return; 
        }

        DateTime checkedAt = DateTime.Now;
        if (checkedAt < _nextUpdateAt)
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
                        catch (Exception ex) when (InternetConnection.IsInternet() is false)
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
                // TODO: ユーザーに購読更新の停止を通知する
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
                    // TODO: 購読グループの未視聴動画を再生する
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
                        await _messenger.SendNavigationRequestAsync(HohoemaPageType.SubscVideoList, new NavigationParameters(("SubscGroupId", groupId.ToString())));
                    }
                    catch
                    {
                        // 購読グループが既に消されていた場合など                            
                        await _messenger.SendNavigationRequestAsync(HohoemaPageType.SubscVideoList);
                    }
                }
                else
                {
                    await _messenger.SendNavigationRequestAsync(HohoemaPageType.SubscVideoList);
                }
                break;
        }
        
        return isHandled;
    }

    private void TriggerToastNotificationFeedUpdateResult(IEnumerable<SubscriptionFeedUpdateResult> updateResults)
    {
        if (!updateResults.Any()) { return; }

        var resultByGroupId = updateResults
            .Where(x => x.IsSuccessed && x.NewVideos?.Count > 0)
            .GroupBy(x => x.Subscription.Group ?? _subscriptionManager.DefaultSubscriptionGroup, SubscriptionGroupComparer.Default)
            .Where(x => _subscriptionManager.GetSubscriptionGroupProps(x.Key.GroupId).IsToastNotificationEnabled)
            .ToArray();

        if (!resultByGroupId.Any()) { return; }

        ToastSelectionBox box = new ToastSelectionBox(ToastArgumentValue_UserInputKey_SubscGroupId)
        {
            DefaultSelectionBoxItemId = resultByGroupId.First().Key.GroupId.ToString()
        };

        box.Items.Add(new ToastSelectionBoxItem(ToastArgumentValue_UserInputValue_NoSelectSubscGroupId, "All".Translate()));
        foreach (var group in resultByGroupId)
        {        
            box.Items.Add(new ToastSelectionBoxItem(group.Key.GroupId.ToString(), $"{group.Key.Name} - {group.Sum(x => x.NewVideos.Count)}件"));
        }

        var newVideoOwnersText = $"新着動画 {resultByGroupId.Sum(x => x.Sum(x => x.NewVideos.Count))}件";
        _notificationService.ShowToast(
            newVideoOwnersText,
            string.Join("\n", resultByGroupId.Where(x => x.Any()).Select(x => $"{x.Key.Name} - {x.Sum(x => x.NewVideos.Count)}件")),
            Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long,
            //luanchContent: PlayWithWatchAfterPlaylistParam,
            toastButtons: new IToastButton[] {
                new ToastButton("WatchVideo".Translate(), MakeSubscPlayToastArguments().ToString()),
                new ToastButton("ViewList".Translate(), MakeSubscViewListToastArguments().ToString()),
            },
            toastInputs: new IToastInput[] {
                box
            }
            );
    }

    private void AddToQueue(IEnumerable<SubscriptionFeedUpdateResult> resultItems)
    {
        if (!resultItems.Any()) { return; }

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
        var sources = _subscriptionManager.GetSubscriptions().Take(3);
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
