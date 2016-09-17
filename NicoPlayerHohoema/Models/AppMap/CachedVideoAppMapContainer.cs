using NicoPlayerHohoema.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.AppMap
{
	public class CachedVideoAppMapContainer : SelfGenerateAppMapContainerBase
	{
		public HohoemaApp HohoemaApp { get; private set; }

		public CachedVideoAppMapContainer(HohoemaApp hohoemaApp)
			: base(HohoemaPageType.CacheManagement, label:"キャッシュ動画")
		{
			HohoemaApp = hohoemaApp;
		}

		protected override Task<IEnumerable<IAppMapItem>> GenerateItems(int count)
		{
			var items = new List<IAppMapItem>();
			var cacheReq = HohoemaApp.MediaManager.CacheRequestedItemsStack.Take(count);
			foreach (var req in cacheReq)
			{
				var videoInfo = Db.VideoInfoDb.Get(req.RawVideoid);
				if (videoInfo == null)
				{
					throw new Exception();
				}

				var item = new CachedVideoAppMapItem(req, videoInfo);
				items.Add(item);
			}

			return Task.FromResult(items.AsEnumerable());
		}
	}

	public class CachedVideoAppMapItem : IAppMapItem
	{
		public string PrimaryLabel { get; private set; }
		public string SecondaryLabel { get; private set; }
		public string Parameter { get; private set; }

		public HohoemaPageType PageType => HohoemaPageType.VideoPlayer;

		public CachedVideoAppMapItem(NicoVideoCacheRequest cacheReq, NicoVideoInfo info)
		{
			PrimaryLabel = info.Title;
			SecondaryLabel = cacheReq.Quality.ToString();
			Parameter = new VideoPlayPayload()
			{
				VideoId = cacheReq.RawVideoid,
				Quality = cacheReq.Quality
			}
			.ToParameterString();
		}
	}
}
