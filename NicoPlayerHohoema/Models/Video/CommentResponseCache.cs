using Mntone.Nico2.Videos.Comment;
using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
	public class CommentResponseCache : Util.Cacheable<CommentResponse>
	{
		public HohoemaApp HohoemaApp { get; private set; }

		public WatchApiResponseCache WatchApiResponseCache { get; private set; }


		public CommentResponseCache(WatchApiResponseCache watchApiCache, HohoemaApp hohoemaApp, StorageFolder saveFolder, string filename) 
			: base(saveFolder, filename)
		{
			WatchApiResponseCache = watchApiCache;
			HohoemaApp = hohoemaApp;
		}

		protected override async Task<CommentResponse> GetLatest()
		{
			var watchApiResopnse = await WatchApiResponseCache.GetItem();

			CommentResponse comment = null;
			try
			{
				comment = await ConnectionRetryUtil.TaskWithRetry(async () =>
				{
					return await this.HohoemaApp.NiconicoContext.Video
						.GetCommentAsync(watchApiResopnse);
				});
			}
			catch { }

			return comment;
		}

	}
}
