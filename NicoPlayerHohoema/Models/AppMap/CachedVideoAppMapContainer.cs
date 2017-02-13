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
		public CachedVideoAppMapContainer()
			: base(HohoemaPageType.CacheManagement, label:"キャッシュ動画")
		{
		}

		protected override Task<IEnumerable<IAppMapItem>> GenerateItems(int count)
		{
			var items = new List<IAppMapItem>();
			var cacheReq = HohoemaApp.MediaManager.CacheVideos.Take(count);
			foreach (var req in cacheReq)
			{
				var videoInfo = Db.VideoInfoDb.Get(req.RawVideoId);
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

	public class CachedVideoAppMapItem : VideoAppMapItemBase
    {
		public CachedVideoAppMapItem(NicoVideo nicoVideo, NicoVideoInfo info)
		{
			PrimaryLabel = info.Title;
            Parameter = nicoVideo.RawVideoId;

            foreach (var divided in nicoVideo.GetAllQuality())
            {
                if (divided.IsCached)
                {
                    SecondaryLabel = divided.Quality.ToString();
                    Quality = divided.Quality;

                    break;
                }
            }
		}
	}
}
