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
		public event Action BeginLoading;
		public event Action CompleteLoading;


		public IncrementalLoadingCollection(T source, uint itemsPerPage = 20)
		{
			this._Source = source;
			this._ItemsPerPage = itemsPerPage;
			this._HasMoreItems = true;
			_Position = 1;
			IsPuaseLoading = false;
		}

		

		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			var dispatcher = Window.Current.Dispatcher;

			
			return Task.Run<LoadMoreItemsResult>(
				async () =>
				{
					await dispatcher.RunAsync(
						CoreDispatcherPriority.Normal,
						() =>
						{
							BeginLoading?.Invoke();
						});

					uint resultCount = 0;

					var result = await _Source.GetPagedItems(_Position, _ItemsPerPage);

					var array = result?.ToList();


					if (array == null || array.Count == 0)
					{
						_HasMoreItems = false;
					}
					else
					{
						resultCount = (uint)array.Count;
					}

					_Position += resultCount;

					Task.WaitAll(
						Task.Delay(3000),
						dispatcher.RunAsync(
							CoreDispatcherPriority.Normal,
							() =>
							{
								foreach (I item in array)
									this.Add(item);
							})
							.AsTask()
						);

					await dispatcher.RunAsync(
						CoreDispatcherPriority.Normal,
						() =>
						{
							CompleteLoading?.Invoke();
						});
			

					return new LoadMoreItemsResult() { Count = resultCount };

				}).AsAsyncOperation();
		}


		protected override void ClearItems()
		{
			base.ClearItems();

			_Position = 1;
		}

		public bool HasMoreItems
		{
			get { return !IsPuaseLoading && _HasMoreItems; }
		}


		public bool IsPuaseLoading { get; set; }


		private T _Source;
		private uint _ItemsPerPage;
		private bool _HasMoreItems;
		private uint _Position;
	}
}
