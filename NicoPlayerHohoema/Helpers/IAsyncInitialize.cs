using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace NicoPlayerHohoema.Helpers
{
    public interface IAsyncInitialize 
    {
        bool IsInitialized { get; }

        Task Initialize();

        void Cancel();
    }


    public abstract class AsyncInitialize : BindableBase, IAsyncInitialize
    {
        public bool IsInitialized { get; private set; } = false;

        AsyncLock _InitializeLock = new AsyncLock();

        public bool IsCancel { get; private set; }

        CancellationTokenSource _CancellationTokenSource;

        public async Task Initialize()
        {
            using (var releaser = await _InitializeLock.LockAsync())
            {
                if (IsInitialized) { return; }

                await InitializeAsync();
            }
        }

        async Task InitializeAsync()
        {
            _CancellationTokenSource = new CancellationTokenSource();

            try
            {
                var ct = _CancellationTokenSource.Token;

                await OnInitializeAsync(ct);

                IsInitialized = true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Initialize Canceled");
            }
            finally
            {
                _CancellationTokenSource?.Dispose();
                _CancellationTokenSource = null;
            }
        }


        public void Cancel()
        {
            _CancellationTokenSource?.Cancel();

            Debug.WriteLine("Initialize Cancel Requested");
        }

        protected abstract Task OnInitializeAsync(CancellationToken token);

    }
}
