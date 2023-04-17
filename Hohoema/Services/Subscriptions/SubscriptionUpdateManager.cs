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

public sealed class SubscriptionUpdateManager 
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

    private TimeSpan _updateFrequency;
    public TimeSpan UpdateFrequency
    {
        get { return _updateFrequency; }
        set 
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (SetProperty(ref _updateFrequency, value))
            {
                _subscriptionSettings.SubscriptionsUpdateFrequency = value;
                StartOrResetTimer();
            }
        }
    }


    public SubscriptionUpdateManager(
        ILoggerFactory loggerFactory,
        IMessenger messenger,
        INotificationService notificationService,
        SubscriptionManager subscriptionManager,
        SubscriptionSettings subscriptionSettingsRepository
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

        _updateFrequency = _subscriptionSettings.SubscriptionsUpdateFrequency;
        _IsAutoUpdateEnabled = _subscriptionSettings.IsSubscriptionAutoUpdateEnabled;

        StartOrResetTimer();
    }


    async ValueTask ISuspendAndResumeAware.OnSuspendingAsync()
    {
        try
        {
            _timerUpdateCancellationTokenSource?.Cancel();
            _timerUpdateCancellationTokenSource = null;
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

    private async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
    {
        var deferral = e.SuspendingOperation.GetDeferral();

    }

    private async void Current_Resuming(object sender, object e)
    {
        
    }

    public async Task UpdateIfOverExpirationAsync(CancellationToken ct)
    {
        if (Helpers.InternetConnection.IsInternet() is false) 
        {
            return; 
        }

        using (_logger.BeginScope("Subscription Update"))
        {
            try
            {
                _logger.ZLogDebug("Start.");
                DateTime checkedAt = DateTime.Now;
                _timerUpdateCancellationTokenSource?.Cancel();
                _timerUpdateCancellationTokenSource?.Dispose();
                _timerUpdateCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(180));
                var timeCt = _timerUpdateCancellationTokenSource.Token;
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeCt))
                using (await _timerLock.LockAsync())
                {
                    List<SubscriptionFeedUpdateResult> updateResultItems = new();
                    foreach (var subscription in _subscriptionManager.GetSortedSubscriptions())
                    {
                        _logger.ZLogDebug("Start. Label: {0}", subscription.Label);
                        var update = _subscriptionManager.GetSubscriptionProps(subscription.SubscriptionId);
                        if (_subscriptionManager.CheckCanUpdate(isManualUpdate: false, subscription, update) is not null and SubscriptionFeedUpdateFailedReason failedReason)
                        {
                            _logger.ZLogDebug("Skiped. Label: {0}, Reason: {1}", subscription.Label, failedReason);
                            continue;
                        }
                        var list = await _subscriptionManager.UpdateSubscriptionFeedVideosAsync(subscription, update: update, checkedAt, linkedCts.Token);
                        updateResultItems.Add(list);

                        _logger.ZLogDebug("Done. Label: {0}", subscription.Label);
                    }

                    // 次の自動更新周期を延長して設定
                    _subscriptionSettings.SubscriptionsLastUpdatedAt = DateTime.Now;

                    NotifyFeedUpdateResult(updateResultItems.Where(x => x.IsSuccessed));
                }
            }
            catch (OperationCanceledException)
            {
                await StopTimerAsync();

                // TODO: ユーザーに購読更新の停止を通知する
                _logger.ZLogInformation("購読の更新にあまりに時間が掛かったため処理を中断し、また定期自動更新も停止しました");
            }
            finally
            {
                _timerUpdateCancellationTokenSource?.Dispose();
                _timerUpdateCancellationTokenSource = null;
                _logger.ZLogDebug("Complete.");
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
                    var lastCheckAt = _subscriptionManager.GetLastCheckedAt(groupId);
                    var recentVideoInUnchecked = _subscriptionManager.GetSubscFeedVideosNewerAt(groupId, lastCheckAt).FirstOrDefault();
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

    private void NotifyFeedUpdateResult(IEnumerable<SubscriptionFeedUpdateResult> updateResults)
    {
        if (!updateResults.Any()) { return; }

        //SubscriptionGroup _defaultSubscGroup = new SubscriptionGroup(SubscriptionGroupId.DefaultGroupId, "SubscGroup_DefaultGroupName".Translate());
        var resultByGroupId = updateResults.Where(x => x.IsSuccessed && x.NewVideos.Count > 0).GroupBy(x => x.Entity.Group ?? _subscriptionManager.DefaultSubscriptionGroup, SubscriptionGroupComparer.Default);

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

#if DEBUG
    public void TestNotification()
    {
        List<SubscriptionFeedUpdateResult> results = new();
        var sources = _subscriptionManager.GetSubscriptions().Take(3);
        foreach (var source in sources)
        {
            var videos = _subscriptionManager.GetSubscFeedVideos(source, 0, 5).Select(x => new NicoVideo() { Id = x.VideoId, Title = x.Title }).ToList();

            results.Add(new SubscriptionFeedUpdateResult() 
            {
                Videos = videos,
                NewVideos = videos,
                Entity = source,
                IsSuccessed = true,
            });
        }

        NotifyFeedUpdateResult(results);
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
