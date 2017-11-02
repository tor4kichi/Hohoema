using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;

namespace NicoPlayerHohoema.ViewModels
{
	public abstract class HohoemaIncrementalSourceBase<T> : IIncrementalSource<T>
	{
        AsyncLock _PageLoadingLock = new AsyncLock();


		public const uint DefaultOneTimeLoadCount = 10;

		public virtual uint OneTimeLoadCount => DefaultOneTimeLoadCount;

		public async Task<IAsyncEnumerable<T>> GetPagedItems(int head, int count)
		{
            using (var releaser = await _PageLoadingLock.LockAsync())
            {
                try
                {
                    var result = await GetPagedItemsImpl(head, count);
                    HasError = false;
                    return result;
                }
                catch
                {
                    Error?.Invoke();
                    return AsyncEnumerable.Empty<T>();
                }
            }
        }

		public async Task<int> ResetSource()
		{
            using (var releaser = await _PageLoadingLock.LockAsync())
            {
                HasError = false;
                try
                {
                    return await ResetSourceImpl();
                }
                catch
                {
                    Error?.Invoke();
                    return await Task.FromResult(0);
                }
            }
        }

		protected abstract Task<IAsyncEnumerable<T>> GetPagedItemsImpl(int head, int count);
		protected abstract Task<int> ResetSourceImpl();

		public bool HasError { get; private set; }
		public event Action Error;

		protected void TriggerError(Exception ex)
		{
			HasError = true;
			Error?.Invoke();
		}
	}
    
}
