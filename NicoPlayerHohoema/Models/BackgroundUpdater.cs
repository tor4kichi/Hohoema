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

	abstract public class BackgroundUpdateItemBase : BindableBase, IBackgroundUpdateable
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
		private SemaphoreSlim _ScheduleUpdateLock;

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

			_ScheduleUpdateLock = new SemaphoreSlim(1, 1);
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
			if (await CheckTaskRunning())
			{
				// 既にタスクが実行中なので何もしない
				return;
			}


			// タスクの開始処理
			var nextItem = await PopItem();
			if (nextItem != null)
			{
				await RegistrationRunningTask(nextItem);
			}
		}

		private async Task<bool> CheckTaskRunning()
		{
			try
			{
				await _ScheduleUpdateLock.WaitAsync();

				return _CurrentUpdateTargetTask != null;
			}
			finally
			{
				_ScheduleUpdateLock.Release();
			}
		}


		private async Task RegistrationRunningTask(IBackgroundUpdateable item)
		{
			try
			{
				await _ScheduleUpdateLock.WaitAsync();

				CurrentUpdateTarget = item;
				_CancelTokenSource = new CancellationTokenSource();
				_CurrentUpdateTargetTask = item.Update().AsTask(_CancelTokenSource.Token)
					.ContinueWith(OnTaskComplete);

				// キャンセル時の処理
				_CancelTokenSource.Token.Register(OnTaskCanceled);

				Debug.WriteLine($"BGTask[{Id}]: begining update {CurrentUpdateTarget.Label}");

				BackgroundUpdateStartedEvent?.Invoke(item);
			}
			finally
			{
				_ScheduleUpdateLock.Release();
			}
		}

		private async void OnTaskCanceled()
		{
			try
			{
				await _ScheduleUpdateLock.WaitAsync();

				if (CurrentUpdateTarget == null) { return; }

				Debug.WriteLine($"BGTask[{Id}]: update complete {CurrentUpdateTarget.Label}");

				BackgroundUpdateCanceledEvent?.Invoke(CurrentUpdateTarget);

				// タスク管理のクリーンナップ
				CurrentUpdateTarget = null;
				_CancelTokenSource?.Dispose();
				_CancelTokenSource = null;
				_CurrentUpdateTargetTask = null;
			}
			finally
			{
				_ScheduleUpdateLock.Release();
			}

		}

		private async Task OnTaskComplete(Task task)
		{
			try
			{
				await _ScheduleUpdateLock.WaitAsync();

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
			finally
			{
				_ScheduleUpdateLock.Release();
			}

			await TryBeginNext();
		}


		#region Schedule Item Stack Management

		private async Task PushItem(IBackgroundUpdateable item, bool pushToTop)
		{
			try
			{
				await _ScheduleUpdateLock.WaitAsync();

				if (pushToTop)
				{
					UpdateTargetStack.Insert(0, item);
				}
				else
				{
					UpdateTargetStack.Add(item);
				}
			}
			finally
			{
				_ScheduleUpdateLock.Release();
			}
		}

		private async Task<bool> HasNextItem()
		{
			try
			{
				await _ScheduleUpdateLock.WaitAsync();

				return UpdateTargetStack.Count > 0;
			}
			finally
			{
				_ScheduleUpdateLock.Release();
			}
		}

		private async Task<IBackgroundUpdateable> PopItem()
		{
			try
			{
				await _ScheduleUpdateLock.WaitAsync();

				var item = UpdateTargetStack.FirstOrDefault();
				if (item != null)
				{
					UpdateTargetStack.Remove(item);
				}

				return item;
			}
			finally
			{
				_ScheduleUpdateLock.Release();
			}
		}

		
		#endregion

	}
}
