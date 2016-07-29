using Mntone.Nico2;
using Mntone.Nico2.Videos.WatchAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
	public class WatchApiResponseCache : Util.Cacheable<WatchApiResponse>
	{
		public HohoemaApp HohoemaApp { get; private set; }

		public string RawVideoId { get; private set; }

		public bool NowLowQualityOnly { get; private set; }

		public bool IsBlockedHarmfulVideo { get; private set; }



		public HarmfulContentReactionType HarmfulContentReactionType { get; set; }

		public bool ForceLowQuality { get; set; }


		public WatchApiResponseCache(string rawVideoId, HohoemaApp hohoemaApp, StorageFolder saveFolder, string filename) 
			: base(saveFolder, filename)
		{
			RawVideoId = rawVideoId;
			HohoemaApp = hohoemaApp;
		}


		protected override async Task<WatchApiResponse> GetLatest()
		{
			IsBlockedHarmfulVideo = false;

			WatchApiResponse watchApiRes = null;
			try
			{
				watchApiRes = await Util.ConnectionRetryUtil.TaskWithRetry(async () =>
				{
					return await HohoemaApp.NiconicoContext.Video.GetWatchApiAsync(
						RawVideoId
						, forceLowQuality: ForceLowQuality
						, harmfulReactType: HarmfulContentReactionType
						);
				});
			}
			catch (AggregateException ea) when (ea.Flatten().InnerExceptions.Any(e => e is ContentZoningException))
			{
				IsBlockedHarmfulVideo = true;
			}
			catch (ContentZoningException)
			{
				IsBlockedHarmfulVideo = true;
			}
			catch { }

			return watchApiRes;
		}

		protected override void UpdateToLatest(WatchApiResponse item)
		{
			if (!ForceLowQuality)
			{
				NowLowQualityOnly = item.VideoUrl.AbsoluteUri.EndsWith("low");
			}

			base.UpdateToLatest(item);
		}
	}
}
