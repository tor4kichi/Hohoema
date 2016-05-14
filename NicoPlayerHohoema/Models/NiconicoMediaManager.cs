using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Util;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	/// <summary>
	/// ニコニコ動画の動画やサムネイル画像、
	/// 動画情報など動画に関わるメディアを管理します
	/// </summary>
	public class NiconicoMediaManager : BindableBase
	{
		internal NiconicoMediaManager(HohoemaApp app)
		{
			_HohoemaApp = app;

			VideoIdToThumbnailInfo = new Dictionary<string, ThumbnailResponse>();
		}



		public async Task<ThumbnailResponse> GetThumbnail(string videoId)
		{
			if (VideoIdToThumbnailInfo.ContainsKey(videoId))
			{
				var value = VideoIdToThumbnailInfo[videoId];

				// TODO: サムネイル情報が古い場合は更新する

				return value;
			}
			else
			{
				var thumbnail = await ConnectionRetryUtil.TaskWithRetry(async () =>
					{
						return await _HohoemaApp.NiconicoContext.Video.GetThumbnailAsync(videoId);
					});
				try
				{
					if (!VideoIdToThumbnailInfo.ContainsKey(videoId))
					{
						VideoIdToThumbnailInfo.Add(videoId, thumbnail);
					}
				}
				catch { }

				return thumbnail;
			}
		}

		public Dictionary<string, ThumbnailResponse> VideoIdToThumbnailInfo { get; private set; }


		HohoemaApp _HohoemaApp;
	}
}
