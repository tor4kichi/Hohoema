using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Helpers;
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

namespace Hohoema.Presentation.ViewModels
{
	public abstract class HohoemaIncrementalSourceBase<T> : IIncrementalSource<T>
	{
        AsyncLock _PageLoadingLock = new AsyncLock();


		public const uint DefaultOneTimeLoadCount = 10;

		public virtual uint OneTimeLoadCount => DefaultOneTimeLoadCount;

		public async IAsyncEnumerable<T> GetPagedItems(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
		{
            using (var releaser = await _PageLoadingLock.LockAsync())
            {
                await foreach (var item in GetPagedItemsImpl(head, count, ct))
                {
                    yield return item;
                }
            }
        }

		public async ValueTask<int> ResetSource()
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

		protected abstract IAsyncEnumerable<T> GetPagedItemsImpl(int head, int count, CancellationToken ct);
		protected abstract ValueTask<int> ResetSourceImpl();

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

        protected override async IAsyncEnumerable<T> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation]CancellationToken ct = default)
        {
            if (head == 0)
            {
                foreach (var item in Source)
                {
                    yield return item;
                }
            }
        }

        protected override ValueTask<int> ResetSourceImpl()
        {
            return new ValueTask<int>((int)OneTimeLoadCount);
        }
    }
}
