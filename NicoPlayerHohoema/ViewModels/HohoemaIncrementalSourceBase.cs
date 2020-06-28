using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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

		public IAsyncEnumerable<T> GetPagedItems(int head, int count, CancellationToken cancellationToken)
		{
//            using (var releaser = await _PageLoadingLock.LockAsync())
            {
                try
                {
                    var result = GetPagedItemsImpl(head, count, cancellationToken);
                    HasError = false;
                    return result;
                }
                catch
                {
                    Error?.Invoke();
                    throw;
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

		protected abstract IAsyncEnumerable<T> GetPagedItemsImpl(int head, int count, CancellationToken cancellationToken);
		protected abstract Task<int> ResetSourceImpl();

		public bool HasError { get; private set; }
		public event Action Error;

		protected void TriggerError(Exception ex)
		{
			HasError = true;
			Error?.Invoke();
		}
	}



    public class ImmidiateIncrementalLoadingCollectionSource<T> : HohoemaIncrementalSourceBase<T>
    {
        private IEnumerable<T> Source { get; }
        public ImmidiateIncrementalLoadingCollectionSource(IEnumerable<T> source)
        {
            Source = source;
            OneTimeLoadCount = (uint)source.Count();
        }

        public override uint OneTimeLoadCount { get; }

        protected override IAsyncEnumerable<T> GetPagedItemsImpl(int head, int count, CancellationToken cancellationToken)
        {
            if (head == 0)
            {
                return Source.ToAsyncEnumerable();
            }
            else
            {
                return Enumerable.Empty<T>().ToAsyncEnumerable();
            }
        }

        protected override Task<int> ResetSourceImpl()
        {
            return Task.FromResult((int)OneTimeLoadCount);
        }
    }
}
