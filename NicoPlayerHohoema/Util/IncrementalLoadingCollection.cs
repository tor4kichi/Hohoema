using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Util
{
	public interface IIncrementalSource<T>
	{
		Task<IEnumerable<T>> GetPagedItems(uint pageIndex, uint pageSize);
	}

	public class IncrementalLoadingCollection<T, I> : ObservableCollection<I>,
			ISupportIncrementalLoading
			where T : IIncrementalSource<I>
	{
		private T source;
		private uint itemsPerPage;
		private bool hasMoreItems;
		private uint currentPage;

		public IncrementalLoadingCollection(T source, uint itemsPerPage = 20)
		{
			this.source = source;
			this.itemsPerPage = itemsPerPage;
			this.hasMoreItems = true;
		}

		public bool HasMoreItems
		{
			get { return hasMoreItems; }
		}

		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			var dispatcher = Window.Current.Dispatcher;

			currentPage = count;

			return Task.Run<LoadMoreItemsResult>(
				async () =>
				{
					uint resultCount = 0;

					var result = await source.GetPagedItems(count, itemsPerPage);

					if (result == null || result.Count() == 0)
					{
						hasMoreItems = false;
					}
					else
					{
						resultCount = (uint)result.Count();
					}

					Task.WaitAll(
						Task.Delay(1000),
						dispatcher.RunAsync(
							CoreDispatcherPriority.Normal,
							() =>
							{
								foreach (I item in result)
									this.Add(item);

							})
							.AsTask()
						);
					
					return new LoadMoreItemsResult() { Count = resultCount };

				}).AsAsyncOperation();
		}
	}
}
