using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Hohoema.Models.Domain.Helpers
{
	public interface IIncrementalSource<T>
	{
		uint OneTimeLoadCount { get; }
		ValueTask<int> ResetSource();
		IAsyncEnumerable<T> GetPagedItems(int head, int count, CancellationToken ct = default);
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
		private bool _HasMoreItems;
		private uint _Position;


        static AsyncLock LoadingLock { get; } = new AsyncLock();

        CoreDispatcher _UIDispatcher;

		CancellationTokenSource _cts = new CancellationTokenSource();

		public IncrementalLoadingCollection(T source)
		{
			this._Source = source;
			this._HasMoreItems = true;
			_Position = 0;
			IsPuaseLoading = false;
            _UIDispatcher = Window.Current.Dispatcher;
        }

		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
            // 多重読み込み防止のため
            // リスト表示に反映されるまで
            // タスクの終了を遅延させる必要があります
            return LoadDataAsync(count, _cts.Token)
				.AsAsyncOperation();

        }

        public async Task<LoadMoreItemsResult> LoadDataAsync(uint count, CancellationToken ct)
        {
			using (await LoadingLock.LockAsync())
			{
				uint resultCount = 0;

				BeginLoading?.Invoke();

				ct.ThrowIfCancellationRequested();

				try
				{
					await foreach (var item in _Source.GetPagedItems((int)_Position, (int)_Source.OneTimeLoadCount, ct).WithCancellation(ct))
					{
						Add(item);
						resultCount++;

						ct.ThrowIfCancellationRequested();
					}
				}
				catch (OperationCanceledException)
				{
					_HasMoreItems = false;
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					_HasMoreItems = false;
				}

				_Position += resultCount;

				if (resultCount == 0)
				{
					_HasMoreItems = false;
				}

				DoneLoading?.Invoke();
				return new LoadMoreItemsResult() { Count = resultCount };
			}
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
			_Position = 0;		
		}

		public void StopLoading()
        {
			_cts.Cancel();
			_cts.Dispose();

			_cts = new CancellationTokenSource();
		}

		public void Dispose()
		{
			_cts.Cancel();
			_cts.Dispose();

			foreach (var item in this)
			{
                (item as IDisposable)?.Dispose();
            }

            (Source as IDisposable)?.Dispose();
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
