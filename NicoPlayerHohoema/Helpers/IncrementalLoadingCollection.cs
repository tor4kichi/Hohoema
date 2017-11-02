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

namespace NicoPlayerHohoema.Helpers
{
	public interface IIncrementalSource<T>
	{
		uint OneTimeLoadCount { get; }
		Task<int> ResetSource();
		Task<IAsyncEnumerable<T>> GetPagedItems(int head, int count);
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

		private SemaphoreSlim _LoadingLock;

		public IncrementalLoadingCollection(T source)
		{
			this._Source = source;
			this._HasMoreItems = true;
			_Position = 0;
			IsPuaseLoading = false;
			_LoadingLock = new SemaphoreSlim(1, 1);
		}

		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
            var dispatcher = Window.Current.Dispatcher;
            return AsyncInfo.Run(async (token) =>
            {
                try
                {
                    await _LoadingLock.WaitAsync();

                    BeginLoading?.Invoke();

                    uint resultCount = 0;

                    try
                    {
                        var items = await _Source.GetPagedItems((int)_Position, (int)_Source.OneTimeLoadCount);
                        
                        if (items == null || (!await items.Any()))
                        {
                            _HasMoreItems = false;
                        }
                        else
                        {
                            _Position += _Source.OneTimeLoadCount;

                            await items.ForEachAsync(async (item) =>
                            {
                                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.Add(item); });
                            });

                            resultCount = (uint)await items.Count();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }

                    

                    // 多重読み込み防止のため
                    // リスト表示に反映されるまで
                    // タスクの終了を遅延させる必要があります
                    await Task.Delay(500);

                    DoneLoading?.Invoke();

                    return new LoadMoreItemsResult() { Count = resultCount };
                }
                finally
                {
                    _LoadingLock.Release();
                }
            }
            );
                
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

		public void Dispose()
		{
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
