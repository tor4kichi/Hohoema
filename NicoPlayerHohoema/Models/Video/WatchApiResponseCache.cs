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
		public static bool NowLowQualityOnly { get; private set; } = true;




		public HohoemaApp HohoemaApp { get; private set; }

		public string RawVideoId { get; private set; }


		public bool IsBlockedHarmfulVideo { get; private set; }



		public HarmfulContentReactionType HarmfulContentReactionType { get; set; }

		public bool ForceLowQuality { get; private set; }

		private bool _NowForceLowQualityUsed;

		public NicoVideoQuality VisitedPageType { get; private set; }

		public MediaProtocolType MediaProtocolType { get; private set; }


		public WatchApiResponseCache(string rawVideoId, HohoemaApp hohoemaApp, StorageFolder saveFolder, string filename) 
			: base(saveFolder, filename)
		{
			RawVideoId = rawVideoId;
			HohoemaApp = hohoemaApp;
		}


		protected override async Task<WatchApiResponse> GetLatest()
		{
			IsBlockedHarmfulVideo = false;

			_NowForceLowQualityUsed = ForceLowQuality;

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

			ForceLowQuality = false;

			return watchApiRes;
		}

		protected override void UpdateToLatest(WatchApiResponse item)
		{
			VisitedPageType = item.VideoUrl.AbsoluteUri.EndsWith("low") ? NicoVideoQuality.Low : NicoVideoQuality.Original;

			if (!_NowForceLowQualityUsed)
			{
				NowLowQualityOnly = VisitedPageType == NicoVideoQuality.Low;
			}

			MediaProtocolType = MediaProtocolTypeHelper.ParseMediaProtocolType(item.VideoUrl);

			base.UpdateToLatest(item);
		}


		public void OnceSetForceLowQualityForcing()
		{
			ForceLowQuality = true;
		}
	}
}
