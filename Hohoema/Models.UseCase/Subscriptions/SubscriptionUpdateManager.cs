﻿using Hohoema.FixPrism;
using Hohoema.Models.Helpers;
using Hohoema.Models.Domain.Subscriptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno;
using Microsoft.AppCenter.Crashes;

namespace Hohoema.Models.UseCase.Subscriptions
{
    // Note: Subscriptionsが０個の場合、自動更新は必要ないが
    // 自動更新周期が長く、また０個であれば処理時間も短く済むため、場合分け処理を入れていない

    public class SubscriptionUpdatedEventArgs
    {
        public DateTime UpdatedTime { get; set; }
        public DateTime NextUpdateTime { get; set; }
    }

    public sealed class SubscriptionUpdateManager : BindableBase, IDisposable
    {
        private readonly SubscriptionManager _subscriptionManager;
        private readonly SubscriptionSettings _subscriptionSettings;
        AsyncLock _timerLock = new AsyncLock();

        IDisposable _timerDisposer;
        bool _isDisposed;


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

        private DateTime _nextUpdateTime;
        public DateTime NextUpdateTime
        {
            get { return _nextUpdateTime; }
            private set { SetProperty(ref _nextUpdateTime, value); }
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
                    NextUpdateTime = _subscriptionSettings.SubscriptionsLastUpdatedAt += _subscriptionSettings.SubscriptionsUpdateFrequency;
                    StartOrResetTimer();
                }
            }
        }


        public SubscriptionUpdateManager(
            SubscriptionManager subscriptionManager,
            SubscriptionSettings subscriptionSettingsRepository
            )
        {
            _subscriptionManager = subscriptionManager;
            _subscriptionSettings = subscriptionSettingsRepository;
            _subscriptionManager.Added += _subscriptionManager_Added;

            _nextUpdateTime = _subscriptionSettings.SubscriptionsLastUpdatedAt + _subscriptionSettings.SubscriptionsUpdateFrequency;
            _updateFrequency = _subscriptionSettings.SubscriptionsUpdateFrequency;
            _IsAutoUpdateEnabled = _subscriptionSettings.IsSubscriptionAutoUpdateEnabled;

            StartOrResetTimer();

            App.Current.Suspending += Current_Suspending;
            App.Current.Resuming += Current_Resuming;
        }

        private async void _subscriptionManager_Added(object sender, SubscriptionSourceEntity e)
        {
            using (await _timerLock.LockAsync())
            {
                using (_timerUpdateCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    await _subscriptionManager.RefreshFeedUpdateResultAsync(e, _timerUpdateCancellationTokenSource.Token);
                }

                _timerUpdateCancellationTokenSource = null;
            }
        }

        private async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            try
            {
                _timerUpdateCancellationTokenSource?.Cancel();
                _timerUpdateCancellationTokenSource = null;
            }
            catch (Exception ex) { ErrorTrackingManager.TrackError(ex); }


            try
            {
                await StopTimerAsync();
            }
            catch (Exception ex) { ErrorTrackingManager.TrackError(ex); }
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
            catch (Exception ex) { ErrorTrackingManager.TrackError(ex); }
        }



        public async Task UpdateAsync(CancellationToken cancellationToken = default)
        {
            using (await _timerLock.LockAsync())
            {
                // ロック中にタイマーが止まっていた場合は更新しない
                if (_timerDisposer == null) { return; }

                // 次の自動更新周期を延長して設定
                _subscriptionSettings.SubscriptionsLastUpdatedAt = DateTime.Now;
                
                NextUpdateTime = _subscriptionSettings.SubscriptionsLastUpdatedAt + _subscriptionSettings.SubscriptionsUpdateFrequency;

                Debug.WriteLine($"[{nameof(SubscriptionUpdateManager)}] start update ------------------- ");
                await _subscriptionManager.RefreshAllFeedUpdateResultAsync(cancellationToken);
                Debug.WriteLine($"[{nameof(SubscriptionUpdateManager)}] end update ------------------- ");
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

                _timerDisposer?.Dispose();
                _timerUpdateCancellationTokenSource?.Cancel();
                _timerUpdateCancellationTokenSource = null;

                if (!_IsAutoUpdateEnabled) { return; }

                IsRunning = true;
                var updateFrequency = _subscriptionSettings.SubscriptionsUpdateFrequency;
                _timerDisposer = Observable.Timer(_subscriptionSettings.SubscriptionsLastUpdatedAt + updateFrequency, updateFrequency)
                    .Subscribe(async _ =>
                    {
                        try
                        {
                            using (_timerUpdateCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(180)))
                            {
                                await UpdateAsync(_timerUpdateCancellationTokenSource.Token);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            var __ = StopTimerAsync();

                            Debug.WriteLine("購読の更新にあまりに時間が掛かったため処理を中断し、また定期自動更新も停止しました");
                        }

                        _timerUpdateCancellationTokenSource = null;
                    });
            }
        }


        async Task StopTimerAsync()
        {
            using (await _timerLock.LockAsync())
            {
                IsRunning = false;
                _timerDisposer?.Dispose();
                _timerDisposer = null;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) { return; }

            _isDisposed = true;

            _ = StopTimerAsync();

            App.Current.Suspending -= Current_Suspending;
            App.Current.Resuming -= Current_Resuming;
        }
    }
}
