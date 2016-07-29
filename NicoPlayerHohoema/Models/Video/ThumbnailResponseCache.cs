using Mntone.Nico2.Videos.Thumbnail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
	public class ThumbnailResponseCache : Util.Cacheable<ThumbnailResponse>
	{
		public HohoemaApp HohoemaApp { get; private set; }

		public string RawVideoId { get; private set; }

		public bool IsDeleted { get; private set; }


		public uint LowQualityVideoSize { get; private set; }
		public uint OriginalQualityVideoSize { get; private set; }



		public bool IsOriginalQualityOnly { get { return LowQualityVideoSize == 0; } }

		public ThumbnailResponseCache(string rawVideoId, HohoemaApp hohoemaApp, StorageFolder saveFolder, string filename) 
			: base(saveFolder, filename)
		{
			RawVideoId = rawVideoId;
			HohoemaApp = hohoemaApp;
		}

		protected override async Task<ThumbnailResponse> GetLatest()
		{
			ThumbnailResponse res = null;

			try
			{
				res = await Util.ConnectionRetryUtil.TaskWithRetry(async () =>
				{
					return await HohoemaApp.NiconicoContext.Video.GetThumbnailAsync(RawVideoId);
				});
			}
			catch (Exception e) when (e.Message.Contains("delete"))
			{
				IsDeleted = true;
				
			}

			return res;
		}

		protected override void UpdateToLatest(ThumbnailResponse item)
		{
			LowQualityVideoSize = (uint)item.SizeLow;
			OriginalQualityVideoSize = (uint) item.SizeHigh;


			base.UpdateToLatest(item);
		}
	}
}
