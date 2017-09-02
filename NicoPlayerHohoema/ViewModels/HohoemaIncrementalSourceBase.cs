using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Util;
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

    public interface IHohoemaPreloadingIncrementalSource
    {
        int TotalCount { get; }
        string PreloadScheduleLabel { get; }
    }

	public abstract class HohoemaPreloadingIncrementalSourceBase<T> : HohoemaIncrementalSourceBase<T>, IHohoemaPreloadingIncrementalSource
    {
		public HohoemaApp HohoemaApp { get; private set; }
		public int TotalCount { get; private set; }


		public HohoemaPreloadingIncrementalSourceBase(HohoemaApp hohoemaApp, string preloadScheduleLabel)
		{
			HohoemaApp = hohoemaApp;
			PreloadScheduleLabel = preloadScheduleLabel;
		}

        private AsyncLock _LoadingLock = new AsyncLock();

        public string PreloadScheduleLabel { get; private set; }

		protected abstract Task Preload(int start, int count);

		protected abstract Task<int> HohoemaPreloadingResetSourceImpl();
		protected abstract Task<IEnumerable<T>> HohoemaPreloadingGetPagedItemsImpl(int start, int count);




		protected override sealed async Task<IEnumerable<T>> GetPagedItemsImpl(int head, int count)
		{
            using (var releaser = await _LoadingLock.LockAsync())
            {
                var items = await HohoemaPreloadingGetPagedItemsImpl(head, count);

                var tail = head + items.Count();
                if (tail != head && tail < TotalCount)
                {
                    await Preload(head + count, (int)OneTimeLoadCount);
                }

                return items;
            }
		}

		protected override sealed async Task<int> ResetSourceImpl()
		{
            using (var releaser = await _LoadingLock.LockAsync())
            {
                TotalCount = await HohoemaPreloadingResetSourceImpl();

                if (TotalCount > 0)
                {
                    await Preload(0, (int)OneTimeLoadCount);
                }

                return TotalCount;
            }
		}

		

	}


	public abstract class HohoemaVideoPreloadingIncrementalSourceBase<T> : HohoemaPreloadingIncrementalSourceBase<T>
		where T : VideoInfoControlViewModel
	{
		public static int VideoListingBackgroundTaskPriority = 10;

		private List<NicoVideo> _VideoItems;

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
            var timer = new Stopwatch();
            timer.Start();

            try
			{
                var items = await PreloadNicoVideo(start, count);

        		_VideoItems.AddRange(items);
			}
			catch (Exception ex)
			{
				TriggerError(ex);
			}

            timer.Stop();
            System.Diagnostics.Debug.WriteLine($"Preload done: {timer.Elapsed}");

        }


        protected override sealed async Task<IEnumerable<T>> HohoemaPreloadingGetPagedItemsImpl(int head, int count)
		{
            await Task.CompletedTask;

            var timer = new Stopwatch();
            timer.Start();

            var tail = Math.Min(head + count, TotalCount);
			List<T> items = new List<T>();

            for (int i = head; i < tail; i++)
//			foreach (var nicoVideo in _VideoItems.Skip(head).Take(count))
			{
                var nicoVideo = _VideoItems[i];
//				var i = _VideoItems.IndexOf(nicoVideo);
				var vm = NicoVideoToTemplatedItem(nicoVideo, i);
				items.Add(vm);
			}

            timer.Stop();
            System.Diagnostics.Debug.WriteLine($"PreloadPagedItems progress: {timer.Elapsed}");
            timer.Reset();
            timer.Start();


            ScheduleDefferedNicoVideoInitialize(items, head);

            timer.Stop();
            System.Diagnostics.Debug.WriteLine($"PreloadPagedItems done: {timer.Elapsed}");

            return items;
		}




		private async void ScheduleDefferedNicoVideoInitialize(List<T> items, int head)
		{
            await Task.Delay(0);

			var start = head + 1;
			var end = Math.Min(start + items.Count, TotalCount);
            foreach (var item in items)
            {
                var index = items.IndexOf(item);
                var updater = new DefferedNicoVideoVMUpdate<T>(new List<T>() { item });
                HohoemaApp.BackgroundUpdater.InstantBackgroundUpdateScheduling(
                    updater,
                    $"{PreloadScheduleLabel}_" + index,
                    PreloadScheduleLabel,
                    priority: VideoListingBackgroundTaskPriority,
                    label: $"{PreloadScheduleLabel} ({start + index} / {end})"
                    );
            }
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
                    });
                }

            }).AsAsyncAction();
		}
	}
}
