using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Helpers;
using Hohoema.Models.Subscriptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLogger;
using Windows.System;
using CommunityToolkit.Mvvm.Messaging;

namespace Hohoema.Models.UseCase.Subscriptions
{
    // Note: Subscriptionsが０個の場合、自動更新は必要ないが
    // 自動更新周期が長く、また０個であれば処理時間も短く済むため、場合分け処理を入れていない

    public class SubscriptionUpdatedEventArgs
    {
        public DateTime UpdatedTime { get; set; }
        public DateTime NextUpdateTime { get; set; }
    }

    public sealed class SubscriptionUpdateManager : ObservableObject, IDisposable
    {
        private readonly ILogger<SubscriptionUpdateManager> _logger;
        private readonly IMessenger _messenger;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly SubscriptionSettings _subscriptionSettings;
        AsyncLock _timerLock = new AsyncLock();

        bool _isDisposed;
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
            _subscriptionManager = subscriptionManager;
            _subscriptionSettings = subscriptionSettingsRepository;

            _messenger.Register<NewSubscMessage>(this, async (r, m) => 
            {
                using (await _timerLock.LockAsync())
                {
                    if (Helpers.InternetConnection.IsInternet() is false) { return; }

                    using (_timerUpdateCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    {
                        await _subscriptionManager.RefreshFeedUpdateResultAsync(m.Value, _timerUpdateCancellationTokenSource.Token);
                    }

                    _timerUpdateCancellationTokenSource = null;
                }
            });

            _updateFrequency = _subscriptionSettings.SubscriptionsUpdateFrequency;
            _IsAutoUpdateEnabled = _subscriptionSettings.IsSubscriptionAutoUpdateEnabled;

            StartOrResetTimer();

            App.Current.Suspending += Current_Suspending;
            App.Current.Resuming += Current_Resuming;
        }

        private async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

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
                deferral.Complete();
            }
        }

        private async void Current_Resuming(object sender, object e)
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

        public async Task UpdateIfOverExpirationAsync(CancellationToken ct)
        {
            if (Helpers.InternetConnection.IsInternet() is false) { return; }

            try
            {
                _timerUpdateCancellationTokenSource?.Cancel();
                _timerUpdateCancellationTokenSource?.Dispose();
                _timerUpdateCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(180));
                var timeCt = _timerUpdateCancellationTokenSource.Token;
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeCt))
                using (await _timerLock.LockAsync())
                {
                    _logger.ZLogDebug("start update");
                    await _subscriptionManager.RefreshAllFeedUpdateResultAsync(linkedCts.Token);
                    _logger.ZLogDebug("end update");

                    // 次の自動更新周期を延長して設定
                    _subscriptionSettings.SubscriptionsLastUpdatedAt = DateTime.Now;
                }
            }
            catch (OperationCanceledException)
            {
                await StopTimerAsync();

                _logger.ZLogInformation("購読の更新にあまりに時間が掛かったため処理を中断し、また定期自動更新も停止しました");
            }
            finally
            {
                _timerUpdateCancellationTokenSource?.Dispose();
                _timerUpdateCancellationTokenSource = null;
            }
        }

        public void RestartIfTimerNotRunning()
        {
            if (!IsRunning)
            {
                StartOrResetTimer();
            }
        }

        CancellationTokenSource _timerUpdateCancellationTokenSource;

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
            App.Current.Suspending -= Current_Suspending;
            App.Current.Resuming -= Current_Resuming;
        }
    }
}
