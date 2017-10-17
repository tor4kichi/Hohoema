using NicoPlayerHohoema.Helpers;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

    public delegate void BackgroundUpdateCompletedEventHandler(object sender);

	public interface IBackgroundUpdateable
	{
		IAsyncAction BackgroundUpdate(CoreDispatcher uiDispatcher);
        event BackgroundUpdateCompletedEventHandler Completed;
	}

    public abstract class BackgroundUpdateableBase : IBackgroundUpdateable
    {
        public event BackgroundUpdateCompletedEventHandler Completed;

        public abstract IAsyncAction BackgroundUpdate(CoreDispatcher uiDispatcher);

        internal void Complete()
        {
            Completed?.Invoke(this);
        }
    }



    /// <summary>
    /// use BackgroundUpdater.CreateBackgroundUpdateInfo instance method.
    /// </summary>
    public class BackgroundUpdateScheduleHandler
	{
		internal BackgroundUpdateScheduleHandler(
			IBackgroundUpdateable target, 
			BackgroundUpdater owner, 
			string id, 
			string groupId, 
			int priority,
			string label
			)
		{
			Target = target;
			Owner = owner;
			Id = id;
			GroupId = groupId;
			Priority = priority;
			Label = label;
		}

		public IBackgroundUpdateable Target { get; private set; }
		public BackgroundUpdater Owner { get; private set; }
		public string Id { get; private set; }
		public string GroupId { get; private set; }
		public int Priority { get; private set; }
		public string Label { get; private set; }

		private bool _IsRunning;
		private bool _IsLastTaskCompleted;

		public event EventHandler<BackgroundUpdateScheduleHandler> Started;
		public event EventHandler<BackgroundUpdateScheduleHandler> Completed;
		public event EventHandler<BackgroundUpdateScheduleHandler> Canceled;

		private AsyncLock _Lock = new AsyncLock();

		private uint _UpdateCompletedCount = 0;

		internal async void Start(CoreDispatcher uiDispatcher)
		{
			using (var releaser = await _Lock.LockAsync())
			{
                _IsRunning = true;
				_IsLastTaskCompleted = false;
			}

			Started?.Invoke(this, this);
		}

		internal async void Complete(CoreDispatcher uiDispatcher)
		{
			using (var releaser = await _Lock.LockAsync())
			{
				_IsRunning = false;
				_IsLastTaskCompleted = true;
				_UpdateCompletedCount++;
            }

            (Target as BackgroundUpdateableBase)?.Complete();

            Completed?.Invoke(this, this);
		}

		internal async void Cancel(CoreDispatcher uiDispatcher)
		{
			using (var releaser = await _Lock.LockAsync())
			{
				_IsRunning = false;
				_IsLastTaskCompleted = false;
			}

			Canceled?.Invoke(this, this);
		}

		public void ScheduleUpdate()
		{
			Owner.Schedule(this);
		}

		public async void Cancel()
		{
			using (var releaser = await _Lock.LockAsync())
			{
				_IsRunning = false;
				_IsLastTaskCompleted = false;
			}

			Owner.CancelFromId(Id);
		}

		public async Task<bool> WaitUpdate()
		{
			bool isLastTaskCompleted = false;
            try
            {
                using (var source = new CancellationTokenSource(30000))
                {
                    while (true)
                    {
                        using (var releaser = await _Lock.LockAsync())
                        {
                            isLastTaskCompleted = _IsLastTaskCompleted;
                            if (!_IsRunning)
                            {
                                break;
                            }
                        }

                        await Task.Delay(30, source.Token);
                    }
                }
            }
            catch
            {
                _IsRunning = false;
            }

            return isLastTaskCompleted;
		}


		public bool IsOneOrMoreUpdateCompleted => _UpdateCompletedCount >= 1;
	}

	class RunningTaskInfo
	{
		public IBackgroundUpdateable Target { get; set; }
		public BackgroundUpdateScheduleHandler Item;
		public Task CurrentUpdateTargetTask { get; set; }
		public CancellationTokenSource CancelTokenSource { get; set; }
	}


	public class BackgroundUpdater : IDisposable
	{
		public static uint MaxTaskSlotCount = 1;


		private bool _IsActive;
		private bool _IsClosed;

		public string Id { get; private set; }
		public CoreDispatcher UIDispatcher { get; private set; }

		public List<BackgroundUpdateScheduleHandler> UpdateTargetStack { get; private set; }
		private AsyncLock _ScheduleUpdateLock;

		private List<RunningTaskInfo> _RunningTasks;

		public event EventHandler<BackgroundUpdateScheduleHandler> BackgroundUpdateStartedEvent;
		public event EventHandler<BackgroundUpdateScheduleHandler> BackgroundUpdateCompletedEvent;
		public event EventHandler<BackgroundUpdateScheduleHandler> BackgroundUpdateCanceledEvent;


		private Dictionary<IBackgroundUpdateable, BackgroundUpdateScheduleHandler> _UpdateInfoMap;

		public BackgroundUpdater(string id, CoreDispatcher uiDispatcher)
		{
			Id = id;
			UIDispatcher = uiDispatcher;
			UpdateTargetStack = new List<BackgroundUpdateScheduleHandler>();
			_RunningTasks = new List<RunningTaskInfo>();
			_ScheduleUpdateLock = new AsyncLock();
			_UpdateInfoMap = new Dictionary<IBackgroundUpdateable, BackgroundUpdateScheduleHandler>();

			App.Current.Resuming += Current_Resuming;
			App.Current.Suspending += Current_Suspending;

			_IsActive = true;
		}

		public void Dispose()
		{
			_IsClosed = true;

			Stop().ConfigureAwait(false);
		}

		public BackgroundUpdateScheduleHandler RegistrationBackgroundUpdateScheduleHandler(
			IBackgroundUpdateable target, 
			string id, 
			string groupId = null, 
			int priority = 0,
			string label = null
			)
		{
			// Note: ここで予めBGUpdateInfo同士の
			// 依存関係解決の元になる情報を構築することもできる
			BackgroundUpdateScheduleHandler handler;
			if (_UpdateInfoMap.ContainsKey(target))
			{
				handler = _UpdateInfoMap[target];
			}
			else
			{
				handler = new BackgroundUpdateScheduleHandler(target, this, id, groupId, priority, label);
				_UpdateInfoMap.Add(target, handler);
			}

			return handler;
		}

		public BackgroundUpdateScheduleHandler InstantBackgroundUpdateScheduling(
			IBackgroundUpdateable target,
			string id,
			string groupId = null,
			int priority = 0,
			string label = null
			)
		{
			var handler = new BackgroundUpdateScheduleHandler(target, this, id, groupId, priority, label);
			handler.ScheduleUpdate();
			return handler;
		}


		public BackgroundUpdateScheduleHandler GetBackgroundUpdateScheduleHandler(
			IBackgroundUpdateable target
			)
		{
			return _UpdateInfoMap[target];
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


		private async Task Stop()
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

				Debug.WriteLine("bg update acitvated");
			}

			await TryBeginNext();
		}

		public async void Deactivate()
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				_IsActive = false;

				Debug.WriteLine("bg update deacitvated");
			}
		}

		/// <summary>
		/// スケジュール
		/// BackgroundUpdateInfo から呼ばれます
		/// </summary>
		/// <param name="item"></param>
		internal async void Schedule(BackgroundUpdateScheduleHandler item)
		{
			await PushItem(item);

			await TryBeginNext();
		}

		public async void CancelAll()
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				UpdateTargetStack.RemoveAll(x => true);

				foreach (var cancelRunningTask in _RunningTasks.ToArray())
				{
					cancelRunningTask.CancelTokenSource.Cancel();
				}
			}
		}

		public async void CancelFromGroupId(string groupId)
		{
			Debug.WriteLine("cancel bg update : " + groupId);

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
			Debug.WriteLine("cancel bg update : " + idList);

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
				if (_RunningTasks.Count >= BackgroundUpdater.MaxTaskSlotCount)
				{
					return false;
				}
				// 次のタスクと今実行中のタスクの優先度が異なる場合は、開始できない
				var nextTask = UpdateTargetStack.FirstOrDefault();
				var currentTask = _RunningTasks.FirstOrDefault();
				if (currentTask != null && nextTask != null)
				{
					if (currentTask.Item.Priority != nextTask.Priority)
					{
						return false;
					}
				}

				return true;
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

		

		private async void StartTaskAndRegistration(BackgroundUpdateScheduleHandler item)
		{
			var cancelTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
			var taskInfo = new RunningTaskInfo()
			{
				Target = item.Target,
				Item = item,
				CancelTokenSource = cancelTokenSource,
				CurrentUpdateTargetTask =
					Task.Run(async () =>
					{
                        try
                        {
                            await item.Target.BackgroundUpdate(UIDispatcher)
                                .AsTask(cancelTokenSource.Token);
#if DEBUG
                            await Task.Delay(0);
#endif
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }

                        return item;
                    }
					)
					.ContinueWith(OnContinueTask)
			};


			// キャンセル時の処理
			taskInfo.CancelTokenSource.Token.Register(OnTaskCanceled, taskInfo);

			Debug.WriteLine($"BGTask[{Id}]: begining update {taskInfo.Item.Id}");

			(item as BackgroundUpdateScheduleHandler)?.Start(UIDispatcher);

			BackgroundUpdateStartedEvent?.Invoke(this, item);

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

			(taskInfo.Target as BackgroundUpdateScheduleHandler)?.Cancel(UIDispatcher);

			BackgroundUpdateCanceledEvent?.Invoke(this, taskInfo.Item);

			// タスク管理のクリーンナップ
			taskInfo.CancelTokenSource?.Dispose();

			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				_RunningTasks.Remove(taskInfo);
			}

			await TryBeginNext().ConfigureAwait(false);
		}

		private async Task OnContinueTask(Task<BackgroundUpdateScheduleHandler> task)
		{
			if (task.IsCompleted && !task.IsCanceled)
			{
				var updateTarget = task.Result;

				Debug.WriteLine($"BGTask[{Id}]: update {task.Status.ToString()} {updateTarget.Id}");
				(updateTarget as BackgroundUpdateScheduleHandler)?.Complete(UIDispatcher);

				// 完了イベント呼び出し
				BackgroundUpdateCompletedEvent?.Invoke(this, updateTarget);

				using (var releaser = await _ScheduleUpdateLock.LockAsync())
				{
					var taskInfo = _RunningTasks.FirstOrDefault(x => x.Item == updateTarget);
					if (taskInfo != null)
					{
						// タスク管理のクリーンナップ
						taskInfo.CancelTokenSource?.Dispose();

						_RunningTasks.Remove(taskInfo);
					}
				}
			}

            if (!task.IsCanceled)
            {
                await TryBeginNext().ConfigureAwait(false);
            }
        }


		#region Schedule Item Stack Management

		private async Task PushItem(BackgroundUpdateScheduleHandler item)
		{
			using (var releaser = await _ScheduleUpdateLock.LockAsync())
			{
				// 重複登録を防止しつつ、追加処理
				if (UpdateTargetStack.All(x => x != item))
				{
					UpdateTargetStack.Add(item);
					UpdateTargetStack.Sort((a, b) => b.Priority - a.Priority);
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

		private async Task<BackgroundUpdateScheduleHandler> PopItem()
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
