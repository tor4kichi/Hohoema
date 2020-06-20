using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Models.Subscriptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Subscriptions
{
    public sealed class SubscriptionUpdateManager : IDisposable
    {
        private readonly SubscriptionManager _subscriptionManager;


        AsyncLock _timerLock = new AsyncLock();

        IDisposable _timerDisposer;
        bool _isDisposed;
        public SubscriptionUpdateManager(
            SubscriptionManager subscriptionManager
            )
        {
            _subscriptionManager = subscriptionManager;

            StartTimer();

            App.Current.Suspending += Current_Suspending;
            App.Current.Resuming += Current_Resuming;
        }

        private async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            _timerUpdateCancellationTokenSource?.Cancel();
            _timerUpdateCancellationTokenSource = null;

            try
            {
                await StopTimerAsync();
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void Current_Resuming(object sender, object e)
        {
            StartTimer();
        }



        public async Task UpdateAsync(CancellationToken cancellationToken = default)
        {
            using (await _timerLock.LockAsync())
            {
                Debug.WriteLine($"[{nameof(SubscriptionUpdateManager)}] start update ------------------- ");
                await _subscriptionManager.RefreshAllFeedUpdateResultAsync(cancellationToken);
                Debug.WriteLine($"[{nameof(SubscriptionUpdateManager)}] end update ------------------- ");
            }
        }


        CancellationTokenSource _timerUpdateCancellationTokenSource;

        async void StartTimer()
        {
            using (await _timerLock.LockAsync())
            {
                _timerDisposer?.Dispose();
                
                if (_isDisposed) { return; }

                _timerDisposer = Observable.Timer(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1))
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
                _timerDisposer?.Dispose();
            }
        }

        public void Dispose()
        {
            _ = StopTimerAsync();

            App.Current.Suspending -= Current_Suspending;
            App.Current.Resuming -= Current_Resuming;
        }
    }
}
