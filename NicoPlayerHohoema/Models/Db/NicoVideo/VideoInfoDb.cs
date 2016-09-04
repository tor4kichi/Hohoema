using Microsoft.EntityFrameworkCore;
using Mntone.Nico2;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Db
{
	public static class VideoInfoDb
	{
		public static NicoVideoInfo GetEnsureNicoVideoInfo(string rawVideoId)
		{
			using (var db = new NicoVideoDbContext())
			{
				var info = db.VideoInfos.SingleOrDefault(x => x.RawVideoId == rawVideoId);

				if (info == null)
				{
					info = new NicoVideoInfo()
					{
						RawVideoId = rawVideoId,
					};

					db.VideoInfos.Add(info);
					db.SaveChanges();
				}

				return info;
			}
		}

		public static async Task UpdateWithThumbnailAsync(string rawVideoId, ThumbnailResponse thumbnailRes)
		{
			using (var db = new NicoVideoDbContext())
			{
				var info = db.VideoInfos.SingleOrDefault(x => x.RawVideoId == rawVideoId);

				if (info == null)
				{
					throw new Exception();
				}

				info.VideoId = thumbnailRes.Id;
				info.HighSize = (uint)thumbnailRes.SizeHigh;
				info.LowSize = (uint)thumbnailRes.SizeLow;
				info.Length = thumbnailRes.Length;
				info.MovieType = thumbnailRes.MovieType;
				info.PostedAt = thumbnailRes.PostedAt.DateTime;
				info.UserId = thumbnailRes.UserId;
				info.UserType = thumbnailRes.UserType;
				info.Description = thumbnailRes.Description;
				info.ThumbnailUrl = thumbnailRes.ThumbnailUrl.AbsoluteUri;

				info.Title = thumbnailRes.Title;
				info.ViewCount = thumbnailRes.ViewCount;
				info.MylistCount = thumbnailRes.MylistCount;
				info.CommentCount = thumbnailRes.CommentCount;
				info.SetTags(thumbnailRes.Tags.Value.ToList());

				db.VideoInfos.Update(info);

				await db.SaveChangesAsync();
			}
		}


		public static async Task UpdateWithWatchApiResponseAsync(string rawVideoId, WatchApiResponse res)
		{
			using (var db = new NicoVideoDbContext())
			{
				var info = db.VideoInfos.SingleOrDefault(x => x.RawVideoId == rawVideoId);
				
				if (info == null)
				{
					throw new Exception();
				}

				info.DescriptionWithHtml = res.videoDetail.description;

				info.ThreadId = res.ThreadId.ToString();
				info.ViewCount = (uint)res.videoDetail.viewCount.Value;
				info.MylistCount = (uint)res.videoDetail.mylistCount.Value;
				info.ViewCount = (uint)res.videoDetail.commentCount.Value;

				info.PrivateReasonType = res.PrivateReason;

				db.VideoInfos.Update(info);

				await db.SaveChangesAsync();
			}
		}


		public static void Deleted(string rawVideoId)
		{
			using (var db = new NicoVideoDbContext())
			{
				var info = db.VideoInfos.SingleOrDefault(x => x.RawVideoId == rawVideoId);

				if (info != null)
				{
					info.IsDeleted = true;
					db.VideoInfos.Update(info);
					db.SaveChanges();
				}
			}
		}

		public static NicoVideoInfo Get(string rawVideoId)
		{
			using (var db = new NicoVideoDbContext())
			{
				return db.VideoInfos.SingleOrDefault(x => x.RawVideoId == rawVideoId);
			}
		}

		public static Task<NicoVideoInfo> GetAsync(string threadId)
		{
			using (var db = new NicoVideoDbContext())
			{
				return db.VideoInfos.SingleOrDefaultAsync(x => x.RawVideoId == threadId);
			}
		}


		public static void Remove(string rawVideoId)
		{
			var nicoVideoInfo = Get(rawVideoId);

			using (var db = new NicoVideoDbContext())
			{
				db.VideoInfos.Remove(nicoVideoInfo);
				db.SaveChanges();
			}
		}

		public static async Task RemoveAsync(string rawVideoId)
		{
			var nicoVideoInfo = await GetAsync(rawVideoId);

			using (var db = new NicoVideoDbContext())
			{
				db.VideoInfos.Remove(nicoVideoInfo);
				await db.SaveChangesAsync();
			}
		}
	}
}
