using NicoPlayerHohoema.Util;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace NicoPlayerHohoema.Models
{
	public interface IBackgroundUpdateable
	{
		string Label { get; }
		SemaphoreSlim UpdateLock { get; }
		IAsyncAction Update();
	}

	public abstract class BackgroundUpdateItemBase : BindableBase, IBackgroundUpdateable
	{
		public BackgroundUpdateItemBase(string label)
		{
			Label = label;
			UpdateLock = new SemaphoreSlim(1, 1);
		}

		public string Label { get; private set; }
		public SemaphoreSlim UpdateLock { get; private set; }

		public abstract IAsyncAction Update();
	}

	public class SimpleBackgroundUpdate : BackgroundUpdateItemBase
	{
		private Func<Task> _UpdateAction;

		public SimpleBackgroundUpdate(string label, Func<Task> updateActionFactory)
			: base(label)
		{
			_UpdateAction = updateActionFactory;
		}

		public override IAsyncAction Update()
		{
			return AsyncInfo.Run(cancelToken => 
			{
				return _UpdateAction();
			});
		}
	}

	public delegate void BackgroundUpdateStartedEventHandler(IBackgroundUpdateable item);
	public delegate void BackgroundUpdateCompletedEventHandler(IBackgroundUpdateable item);
	public delegate void BackgroundUpdateCanceledEventHandler(IBackgroundUpdateable item);

	public class BackgroundUpdater : IDisposable
	{
		public string Id { get; private set; }
		private bool _IsClosed;

		public List<IBackgroundUpdateable> UpdateTargetStack { get; private set; }
		private AsyncLock _ScheduleUpdateLock;

		public IBackgroundUpdateable CurrentUpdateTarget { get; private set; }
		private Task _CurrentUpdateTargetTask;
		private CancellationTokenSource _CancelTokenSource;


		public event BackgroundUpdateStartedEventHandler BackgroundUpdateStartedEvent;
		public event BackgroundUpdateCompletedEventHandler BackgroundUpdateCompletedEvent;
		public event BackgroundUpdateCanceledEventHandler BackgroundUpdateCanceledEvent;

		public BackgroundUpdater(string id)
		{
			Id = id;
			UpdateTargetStack = new List<IBackgroundUpdateable>();

			_ScheduleUpdateLock = new AsyncLock();

			App.Current.Resuming += Current_Resuming;
			App.Current.Suspending += Current_Suspending;
		}

		private async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();

			Debug.WriteLine($"BGUpdater[{Id}]の処理を中断");
			await PauseBackgroundUpdate();

			deferral.Complete();
		}

		private async void Current_Resuming(object sender, object e)
		{
			Debug.WriteLine($"BGUpdater[{Id}]の処理を再開");
			await TryBeginNext();
		}


		public async Task PauseBackgroundUpdate()
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				if (_CancelTokenSource != null)
				{
					_CancelTokenSource.Cancel();
				}
			}
		}

		public void Dispose()
		{
			_IsClosed = true;

			_CancelTokenSource?.Cancel();
		}


		public async Task Schedule(IBackgroundUpdateable item, bool priorityUpdate = false)
		{
			await PushItem(item, priorityUpdate);

			await TryBeginNext();
		}


		private async Task TryBeginNext()
		{
			if (_IsClosed) { return; }

			if (await CheckTaskRunning())
			{
				// 既にタスクが実行中なので何もしない
				return;
			}

			Debug.WriteLine("bg task check.");

			// タスクの開始処理
			var nextItem = await PopItem();
			if (nextItem != null)
			{
				try
				{
					await RegistrationRunningTask(nextItem);
				}
				catch (OperationCanceledException)
				{
					Debug.WriteLine($"{nextItem.Label} の処理が中断されました。");
					await Schedule(nextItem, true);
				}
			}
		}

		private async Task<bool> CheckTaskRunning()
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				return _CurrentUpdateTargetTask != null;
			}
		}


		private async Task RegistrationRunningTask(IBackgroundUpdateable item)
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				CurrentUpdateTarget = item;
				_CancelTokenSource = new CancellationTokenSource();
				_CurrentUpdateTargetTask =
					Task.Run(async () =>
					{
						await item.Update();
					})
					.ContinueWith(OnTaskComplete);

				// キャンセル時の処理
				_CancelTokenSource.Token.Register(OnTaskCanceled);

				Debug.WriteLine($"BGTask[{Id}]: begining update {CurrentUpdateTarget.Label}");

				BackgroundUpdateStartedEvent?.Invoke(item);
			}
		}

		private async void OnTaskCanceled()
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				if (CurrentUpdateTarget == null) { return; }

				Debug.WriteLine($"BGTask[{Id}]: update complete {CurrentUpdateTarget.Label}");

				BackgroundUpdateCanceledEvent?.Invoke(CurrentUpdateTarget);

				// タスク管理のクリーンナップ
				CurrentUpdateTarget = null;
				_CancelTokenSource?.Dispose();
				_CancelTokenSource = null;
				_CurrentUpdateTargetTask = null;
			}
			
		}

		private async Task OnTaskComplete(Task task)
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				if (CurrentUpdateTarget == null) { return; }

				Debug.WriteLine($"BGTask[{Id}]: update complete {CurrentUpdateTarget.Label}");

				// 完了イベント呼び出し
				BackgroundUpdateCompletedEvent?.Invoke(CurrentUpdateTarget);


				// タスク管理のクリーンナップ
				CurrentUpdateTarget = null;
				_CurrentUpdateTargetTask = null;
				_CancelTokenSource?.Dispose();
				_CancelTokenSource = null;
			}

			await Task.Delay(100);
			await TryBeginNext().ConfigureAwait(false);
		}


		#region Schedule Item Stack Management

		private async Task PushItem(IBackgroundUpdateable item, bool pushToTop)
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				if (pushToTop)
				{
					UpdateTargetStack.Insert(0, item);
				}
				else
				{
					UpdateTargetStack.Add(item);
				}
			}
			
		}

		private async Task<bool> HasNextItem()
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				return UpdateTargetStack.Count > 0;
			}
		}

		private async Task<IBackgroundUpdateable> PopItem()
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				var item = UpdateTargetStack.FirstOrDefault();
				if (item != null)
				{
					UpdateTargetStack.Remove(item);
				}

				return item;
			}
		}

		
		#endregion

	}
}
