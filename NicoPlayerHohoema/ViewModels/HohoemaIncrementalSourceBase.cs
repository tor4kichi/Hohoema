using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	abstract public class HohoemaIncrementalSourceBase<T> : IIncrementalSource<T>
	{
		public const uint DefaultOneTimeLoadCount = 7;

		public virtual uint OneTimeLoadCount => DefaultOneTimeLoadCount;

		public abstract Task<IEnumerable<T>> GetPagedItems(int head, int count);
		public abstract Task<int> ResetSource();
	}



	abstract public class HohoemaPreloadingIncrementalSourceBase<T> : HohoemaIncrementalSourceBase<T>
	{
		public HohoemaApp HohoemaApp { get; private set; }
		public int TotalCount { get; private set; }


		public HohoemaPreloadingIncrementalSourceBase(HohoemaApp hohoemaApp, string preloadScheduleLabel)
		{
			HohoemaApp = hohoemaApp;
			PreloadScheduleLabel = preloadScheduleLabel;
		}


		public string PreloadScheduleLabel { get; private set; }

		abstract protected Task Preload(int start, int count);

		abstract protected Task<int> ResetSourceImpl();
		abstract protected Task<IEnumerable<T>> GetPagedItemsImpl(int start, int count);




		public override sealed async Task<IEnumerable<T>> GetPagedItems(int head, int count)
		{
			var items = await GetPagedItemsImpl(head, count);

			var tail = head + items.Count();
			if (tail != head && tail < TotalCount)
			{
				await SchedulePreloading(head + count, (int)OneTimeLoadCount);
			}

			return items;
		}

		public override sealed async Task<int> ResetSource()
		{
			TotalCount = await ResetSourceImpl();

			if (TotalCount > 0)
			{
				await SchedulePreloading(0, (int)OneTimeLoadCount);
			}

			await Task.Delay(200);

			return TotalCount;
		}

		private Task SchedulePreloading(int start, int count)
		{
			// 先頭20件を先行ロード
			return HohoemaApp.ThumbnailBackgroundLoader.Schedule(
				new SimpleBackgroundUpdate($"{PreloadScheduleLabel}[{start} - {start + count}]"
				, () => Preload(start, count)
				)
				);
		}

	}


	abstract public class HohoemaVideoPreloadingIncrementalSourceBase<T> : HohoemaPreloadingIncrementalSourceBase<T>
	{
		private List<NicoVideo> _VideoItems;

		private AsyncLock _VideoItemsLock = new AsyncLock();

		public HohoemaVideoPreloadingIncrementalSourceBase(HohoemaApp hohoemaApp, string preloadScheduleLabel)
			: base(hohoemaApp, preloadScheduleLabel)
		{
			_VideoItems = new List<NicoVideo>();
		}


		
		abstract protected Task<IEnumerable<string>> PreloadVideoIds(int start, int count);
		abstract protected T NicoVideoToTemplatedItem(NicoVideo sourceNicoVideos, int index);


		protected override async Task Preload(int start, int count)
		{
			var items = await PreloadVideoIds(start, count);

			using (var releaser = await _VideoItemsLock.LockAsync())
			{
				var videos = await HohoemaApp.MediaManager.GetNicoVideoItemsAsync(items.ToArray());
				_VideoItems.AddRange(videos);
			}
		}


		protected override sealed async Task<IEnumerable<T>> GetPagedItemsImpl(int head, int count)
		{
			var tail = head + count;

			while (_VideoItems.Count < tail)
			{
				await Task.Delay(5);
			}

			List<T> items = new List<T>();
			using (var releaser = await _VideoItemsLock.LockAsync())
			{
				for (int i = head; i < tail; i++)
				{
					var nicoVideo = _VideoItems.ElementAt(i);
					var vm = NicoVideoToTemplatedItem(nicoVideo, i);
					items.Add(vm);
				}
			}
			
			return items;
		}


	}
}
