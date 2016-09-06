using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Util
{
	public interface IIncrementalSource<T>
	{
		Task<int> ResetSource();
		Task<IEnumerable<T>> GetPagedItems(int head, int count);
	}

	public class IncrementalLoadingCollection<T, I> : ObservableCollection<I>,
			ISupportIncrementalLoading,
			IDisposable
			where T : IIncrementalSource<I>
	{
		public event Action BeginLoading;
		public event Action DoneLoading;

		// Note: Navigation操作に関わるバグへの対処
		// 読み込み中にナビゲーション等によって ListView の LayoutUpdate が阻害されると
		// IncrementalLoading 処理が呼び出し続けられてしまいます（※未検証）
		// これを防止するため、Page.NavigationTo/From で IsPuaseLoading をスイッチして対応してください
		// なおPage.NavigationFromでIncrementalLoadingCollectionをItemsSourceから外すとより確実に読み込みを一時停止できます
		public bool IsPuaseLoading { get; set; }
		private T _Source;
		private uint _ItemsPerPage;
		private bool _HasMoreItems;
		private uint _Position;

		private SemaphoreSlim _LoadingLock;

		public IncrementalLoadingCollection(T source, uint itemsPerPage = 20, uint firstHeadPosition = 0)
		{
			this._Source = source;
			this._ItemsPerPage = itemsPerPage;
			this._HasMoreItems = true;
			_Position = firstHeadPosition;
			IsPuaseLoading = false;
			_LoadingLock = new SemaphoreSlim(1, 1);
		}

		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			var dispatcher = Window.Current.Dispatcher;

			return Task.Run(async () =>
			{
				try
				{
					await _LoadingLock.WaitAsync();

					await dispatcher.RunAsync(CoreDispatcherPriority.Normal,
					() => BeginLoading?.Invoke()
					);

					uint resultCount = 0;

					List<I> resultItems = null;
					try
					{
						var items = await _Source.GetPagedItems((int)_Position, (int)_ItemsPerPage);
						resultItems = items?.ToList();
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex.Message);
					}

					if (resultItems == null || resultItems.Count == 0)
					{
						_HasMoreItems = false;
					}
					else
					{
						resultCount = (uint)resultItems.Count;

						_Position += resultCount;

						await dispatcher.RunAsync(CoreDispatcherPriority.High,
							() =>
							{
								foreach (I item in resultItems)
									this.Add(item);
							});

					}

					// 多重読み込み防止のため
					// リスト表示に反映されるまで
					// タスクの終了を遅延させる必要があります
					await Task.Delay(500);

					await dispatcher.RunAsync(CoreDispatcherPriority.Normal,
						() => DoneLoading?.Invoke()
						);

					return new LoadMoreItemsResult() { Count = resultCount };
				}
				finally
				{
					_LoadingLock.Release();
				}
				
			})
			.AsAsyncOperation();
		}


		protected override void ClearItems()
		{
			foreach (var item in this)
			{
				if (item is IDisposable)
				{
					(item as IDisposable).Dispose();
				}
			}

			base.ClearItems();

			// Note: PullToRefresh等で要素を削除した時のための対応
			// IIncrementalSourceの実装で head == 1 の時に
			// 強制的にアイテムソースのリストを更新させるよう対応してください
			_Position = 1;		
		}

		public void Dispose()
		{
			foreach (var item in this)
			{
				if (item is IDisposable)
				{
					(item as IDisposable).Dispose();
				}
			}
		}

		public bool HasMoreItems
		{
			get { return !IsPuaseLoading && _HasMoreItems; }
		}

		public T Source
		{
			get
			{
				return _Source;
			}
		}
		

		
	}
}
