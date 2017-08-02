using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;

namespace NicoPlayerHohoema.ViewModels
{
	public abstract class HohoemaIncrementalSourceBase<T> : IIncrementalSource<T>
	{
		public const uint DefaultOneTimeLoadCount = 10;

		public virtual uint OneTimeLoadCount => DefaultOneTimeLoadCount;

		public async Task<IEnumerable<T>> GetPagedItems(int head, int count)
		{
			try
			{
				var result = await GetPagedItemsImpl(head, count);
				HasError = false;
				return result;
			}
			catch
			{
				Error?.Invoke();
				return Enumerable.Empty<T>();
			}
		}

		public Task<int> ResetSource()
		{
			HasError = false;
			try
			{
				return ResetSourceImpl();
			}
			catch
			{
				Error?.Invoke();
				return Task.FromResult(0);
			}
		}

		protected abstract Task<IEnumerable<T>> GetPagedItemsImpl(int head, int count);
		protected abstract Task<int> ResetSourceImpl();

		public bool HasError { get; private set; }
		public event Action Error;

		protected void TriggerError(Exception ex)
		{
			HasError = true;
			Error?.Invoke();
		}
	}



	public abstract class HohoemaPreloadingIncrementalSourceBase<T> : HohoemaIncrementalSourceBase<T>
	{
		public HohoemaApp HohoemaApp { get; private set; }
		public int TotalCount { get; private set; }


		public HohoemaPreloadingIncrementalSourceBase(HohoemaApp hohoemaApp, string preloadScheduleLabel)
		{
			HohoemaApp = hohoemaApp;
			PreloadScheduleLabel = preloadScheduleLabel;
		}


		public string PreloadScheduleLabel { get; private set; }

		protected abstract Task Preload(int start, int count);

		protected abstract Task<int> HohoemaPreloadingResetSourceImpl();
		protected abstract Task<IEnumerable<T>> HohoemaPreloadingGetPagedItemsImpl(int start, int count);




		protected override sealed async Task<IEnumerable<T>> GetPagedItemsImpl(int head, int count)
		{
			var items = await HohoemaPreloadingGetPagedItemsImpl(head, count);

			var tail = head + items.Count();
			if (tail != head && tail < TotalCount)
			{
				await Preload(head + count, (int)OneTimeLoadCount);
			}

			return items;
		}

		protected override sealed async Task<int> ResetSourceImpl()
		{
			TotalCount = await HohoemaPreloadingResetSourceImpl();

			if (TotalCount > 0)
			{
				await Preload(0, (int)OneTimeLoadCount);
			}

			return TotalCount;
		}

		

	}


	public abstract class HohoemaVideoPreloadingIncrementalSourceBase<T> : HohoemaPreloadingIncrementalSourceBase<T>
		where T : VideoInfoControlViewModel
	{
		public static int VideoListingBackgroundTaskPriority = 10;

		private List<NicoVideo> _VideoItems;

		private AsyncLock _VideoItemsLock = new AsyncLock();

		public HohoemaVideoPreloadingIncrementalSourceBase(HohoemaApp hohoemaApp, string preloadScheduleLabel)
			: base(hohoemaApp, preloadScheduleLabel)
		{
			_VideoItems = new List<NicoVideo>();
		}

		protected abstract Task<IEnumerable<NicoVideo>> PreloadNicoVideo(int start, int count);
		protected abstract T NicoVideoToTemplatedItem(NicoVideo sourceNicoVideos, int index);


		protected async Task<NicoVideo> ToNicoVideo(string videoId)
		{
			return await HohoemaApp.MediaManager.GetNicoVideoAsync(videoId, withInitialize:false);
		}

		protected override async Task Preload(int start, int count)
		{
			try
			{
                var items = await PreloadNicoVideo(start, count);

                using (var releaser = await _VideoItemsLock.LockAsync())
                {
					_VideoItems.AddRange(items);
				}
			}
			catch (Exception ex)
			{
				TriggerError(ex);
			}
		}


		protected override sealed async Task<IEnumerable<T>> HohoemaPreloadingGetPagedItemsImpl(int head, int count)
		{
			var tail = Math.Min(head + count, TotalCount);
			List<T> items = new List<T>();

			using (var token = new CancellationTokenSource(10000))
			{
				try
				{
					while (_VideoItems.Count < tail)
					{
						await Task.Delay(10);

						token.Token.ThrowIfCancellationRequested();
					}
				}
				catch (OperationCanceledException ex)
				{
					try
					{
						var result = await PreloadNicoVideo(head, count);

						using (var releaser = await _VideoItemsLock.LockAsync())
						{
							_VideoItems.AddRange(result);
						}
					}
					catch
					{
						throw ex;
					}
				}
			}

			using (var releaser = await _VideoItemsLock.LockAsync())
			{
				foreach (var nicoVideo in _VideoItems.Skip(head).Take(count).ToArray())
				{
					var i = _VideoItems.IndexOf(nicoVideo);
					var vm = NicoVideoToTemplatedItem(nicoVideo, i);
					items.Add(vm);
				}
			}

			ScheduleDefferedNicoVideoInitialize(items, head);

			return items;
		}




		private void ScheduleDefferedNicoVideoInitialize(List<T> items, int head)
		{
			var start = head + 1;
			var end = Math.Min(start + items.Count, TotalCount);
			var updater = new DefferedNicoVideoVMUpdate<T>(items);
			HohoemaApp.BackgroundUpdater.InstantBackgroundUpdateScheduling(
				updater, 
				$"{PreloadScheduleLabel}_" + TotalCount,
				PreloadScheduleLabel,
				priority: VideoListingBackgroundTaskPriority,
				label: $"{PreloadScheduleLabel} ({start} - {end})"
				);
		}

		public void CancelPreloading()
		{
			HohoemaApp.BackgroundUpdater.CancelFromGroupId(PreloadScheduleLabel);
		}
	}

	public class DefferedNicoVideoVMUpdate<T> : BackgroundUpdateableBase
		where T : VideoInfoControlViewModel
	{
		public List<T> Items { get; private set; }
		public DefferedNicoVideoVMUpdate(List<T> items)
		{
			Items = items;
		}

		public override IAsyncAction BackgroundUpdate(CoreDispatcher uiDispatcher)
		{
            return Task.Run(async () =>
            {
                foreach (var item in Items)
                {
                    await item.NicoVideo.Initialize();

                    await uiDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        item.SetupFromThumbnail(item.NicoVideo);
                    })
                    .AsTask().ConfigureAwait(false);
                }

            }).AsAsyncAction();
		}
	}
}
