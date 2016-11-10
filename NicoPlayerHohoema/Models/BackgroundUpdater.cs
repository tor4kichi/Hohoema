using NicoPlayerHohoema.Util;
using Prism.Mvvm;
using Reactive.Bindings;
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

namespace NicoPlayerHohoema.Models
{
	// Note: 優先度付きのUIスレッド協調動作指向バックグラウンド更新

	// BackgroundUpdateItemBaseを更新機能を持つクラスとは別に派生クラスを作成して、
	// その中で対象の更新を処理する

	public delegate void BackgroundUpdateStartedEventHandler(BackgroundUpdateInfo item);
	public delegate void BackgroundUpdateCompletedEventHandler(BackgroundUpdateInfo item);
	public delegate void BackgroundUpdateCanceledEventHandler(BackgroundUpdateInfo item);

	public interface IBackgroundUpdateable
	{
		IAsyncAction BackgroundUpdate(CoreDispatcher uiDispatcher);
	}

	/// <summary>
	/// use BackgroundUpdater.CreateBackgroundUpdateInfo instance method.
	/// </summary>
	public class BackgroundUpdateInfo
	{
		internal BackgroundUpdateInfo(IBackgroundUpdateable target, BackgroundUpdater owner, string id, string groupId, int priority)
		{
			Target = target;
			Owner = owner;
			Id = id;
			GroupId = groupId;
			Priority = priority;
		}

		public IBackgroundUpdateable Target { get; private set; }
		public BackgroundUpdater Owner { get; private set; }
		public string Id { get; private set; }
		public string GroupId { get; private set; }
		public int Priority { get; private set; }

		public event BackgroundUpdateStartedEventHandler Started;
		public event BackgroundUpdateCompletedEventHandler Completed;
		public event BackgroundUpdateCanceledEventHandler Canceled;

		internal void Start(CoreDispatcher uiDispatcher)
		{
			Started?.Invoke(this);
		}

		internal void Complete(CoreDispatcher uiDispatcher)
		{
			Completed?.Invoke(this);
		}

		internal void Cancel(CoreDispatcher uiDispatcher)
		{
			Canceled?.Invoke(this);
		}

		public void ScheduleUpdate()
		{
			Owner.Schedule(this);
		}

		public void Cancel()
		{
			Owner.CancelFromId(Id);
		}

	}

	class RunningTaskInfo
	{
		public IBackgroundUpdateable Target { get; set; }
		public BackgroundUpdateInfo Item;
		public Task CurrentUpdateTargetTask { get; set; }
		public CancellationTokenSource CancelTokenSource { get; set; }
	}


	public class BackgroundUpdater : IDisposable
	{
		public uint TaskSlotCount = 2;


		private bool _IsActive;
		private bool _IsClosed;

		public string Id { get; private set; }
		public CoreDispatcher UIDispatcher { get; private set; }

		public List<BackgroundUpdateInfo> UpdateTargetStack { get; private set; }
		private AsyncLock _ScheduleUpdateLock;

		private List<RunningTaskInfo> _RunningTasks;

		public event BackgroundUpdateStartedEventHandler BackgroundUpdateStartedEvent;
		public event BackgroundUpdateCompletedEventHandler BackgroundUpdateCompletedEvent;
		public event BackgroundUpdateCanceledEventHandler BackgroundUpdateCanceledEvent;

		public BackgroundUpdater(string id, CoreDispatcher uiDispatcher, uint maxTaskSlotCount = 2)
		{
			Id = id;
			TaskSlotCount = maxTaskSlotCount;
			UIDispatcher = uiDispatcher;
			UpdateTargetStack = new List<BackgroundUpdateInfo>();
			_RunningTasks = new List<RunningTaskInfo>();
			_ScheduleUpdateLock = new AsyncLock();

			App.Current.Resuming += Current_Resuming;
			App.Current.Suspending += Current_Suspending;

			_IsActive = true;
		}

		public void Dispose()
		{
			_IsClosed = true;

			Stop().ConfigureAwait(false);
		}

		public BackgroundUpdateInfo CreateBackgroundUpdateInfo(
			IBackgroundUpdateable target, 
			string id, 
			string groupId = null, 
			int priority = 0
			)
		{
			// Note: ここで予めBGUpdateInfo同士の
			// 依存関係解決の元になる情報を構築することもできる

			return new BackgroundUpdateInfo(target, this, id, groupId, priority);
		}

		public BackgroundUpdateInfo CreateBackgroundUpdateInfoWithImmidiateSchedule(
			IBackgroundUpdateable target,
			string id,
			string groupId = null,
			int priority = 0
			)
		{
			// Note: ここで予めBGUpdateInfo同士の
			// 依存関係解決の元になる情報を構築することもできる

			var info = new BackgroundUpdateInfo(target, this, id, groupId, priority);
			info.ScheduleUpdate();
			return info;
		}

		#region Application Lifecycle Handling

		private async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();

			Debug.WriteLine($"BGUpdater[{Id}]の処理を中断");
			await Stop();

			deferral.Complete();
		}

		private async void Current_Resuming(object sender, object e)
		{
			Debug.WriteLine($"BGUpdater[{Id}]の処理を再開");
			await TryBeginNext();
		}


		#endregion

		#region Control background task running


		public async Task Stop()
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				foreach (var task in _RunningTasks)
				{
					task.CancelTokenSource?.Cancel();
				}
			}
		}

		public async Task<bool> CheckActive()
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				return _IsActive;
			}
		}


		public async void Activate()
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				_IsActive = true;
			}

			await TryBeginNext();
		}

		public async void Deactivate()
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				_IsActive = false;
			}
		}

		/// <summary>
		/// スケジュール
		/// BackgroundUpdateInfo から呼ばれます
		/// </summary>
		/// <param name="item"></param>
		internal async void Schedule(BackgroundUpdateInfo item)
		{
			await PushItem(item);

			await TryBeginNext();
		}

		public async void CancelFromGroupId(string groupId)
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				UpdateTargetStack.RemoveAll(x => x.GroupId == groupId);

				foreach (var cancelRunningTask in _RunningTasks.Where(x => x.Item.GroupId == groupId).ToArray())
				{
					cancelRunningTask.CancelTokenSource.Cancel();
				}
			}
		}
		public async void CancelFromId(params string[] idList)
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				UpdateTargetStack.RemoveAll(x => idList.Any(y => y == x.Id));

				foreach (var cancelRunningTask in _RunningTasks.Where(x => idList.Any(y => y == x.Item.Id)).ToArray())
				{
					cancelRunningTask.CancelTokenSource.Cancel();
				}
			}
		}

		public async Task<bool> CanStartBackgroundTask()
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				return _RunningTasks.Count < this.TaskSlotCount;
			}
		}


		#endregion



		private async Task TryBeginNext()
		{
			if (_IsClosed) { return; }

			if (!await CheckActive()) { return; }

			// タスクスロット数が許す限り並列でバックグラウンド処理を開始させる
			while (await CanStartBackgroundTask())
			{
				// タスクの開始処理
				var nextItem = await PopItem();
				if (nextItem == null)
				{
					break;
				}

				try
				{
					StartTaskAndRegistration(nextItem);
				}
				catch (OperationCanceledException)
				{
					Debug.WriteLine($"{nextItem.Id} の処理が中断されました。");
					Schedule(nextItem);
				}
			}
		}

		

		private async void StartTaskAndRegistration(BackgroundUpdateInfo item)
		{
			var taskInfo = new RunningTaskInfo()
			{
				Target = item.Target,
				Item = item,
				CancelTokenSource = new CancellationTokenSource(),
				CurrentUpdateTargetTask =
					Task.Run(async () =>
					{
						await item.Target.BackgroundUpdate(UIDispatcher);
						return item;
					})
					.ContinueWith(OnTaskComplete)
			};


			// キャンセル時の処理
			taskInfo.CancelTokenSource.Token.Register(OnTaskCanceled, taskInfo);

			Debug.WriteLine($"BGTask[{Id}]: begining update {taskInfo.Item.Id}");

			(item as BackgroundUpdateInfo)?.Start(UIDispatcher);

			BackgroundUpdateStartedEvent?.Invoke(item);

			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				_RunningTasks.Add(taskInfo);
			}
			
		}

		private async void OnTaskCanceled(object a)
		{
			var taskInfo = (RunningTaskInfo) a;
			if (taskInfo.Target == null) { return; }

			Debug.WriteLine($"BGTask[{Id}]: update complete {taskInfo.Item.Id}");

			(taskInfo.Target as BackgroundUpdateInfo)?.Cancel(UIDispatcher);

			BackgroundUpdateCanceledEvent?.Invoke(taskInfo.Item);

			// タスク管理のクリーンナップ
			taskInfo.CancelTokenSource?.Dispose();

			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				_RunningTasks.Remove(taskInfo);
			}
			
		}

		private async Task OnTaskComplete(Task<BackgroundUpdateInfo> task)
		{
			var updateTarget = task.Result;

			Debug.WriteLine($"BGTask[{Id}]: update {task.Status.ToString()} {updateTarget.Id}");
			(updateTarget as BackgroundUpdateInfo)?.Complete(UIDispatcher);

			// 完了イベント呼び出し
			BackgroundUpdateCompletedEvent?.Invoke(updateTarget);

			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				var taskInfo = _RunningTasks.First(x => x.Item == updateTarget);
				// タスク管理のクリーンナップ
				taskInfo.CancelTokenSource?.Dispose();

				_RunningTasks.Remove(taskInfo);
			}

			await TryBeginNext().ConfigureAwait(false);
		}


		#region Schedule Item Stack Management

		private async Task PushItem(BackgroundUpdateInfo item)
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				// 重複登録を防止しつつ、追加処理
				if (UpdateTargetStack.All(x => x != item))
				{
					UpdateTargetStack.Add(item);
					UpdateTargetStack.Sort((a, b) => a.Priority - b.Priority);
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

		private async Task<BackgroundUpdateInfo> PopItem()
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
