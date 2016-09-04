using Mntone.Nico2.Videos.Thumbnail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Db
{
	public static class VideoNgDb
	{
		public static NGResult IsNgVideo(string videoTitle, uint userId, IList<Tag> tags)
		{
			using (var db = new NgDbContext())
			{
				if (db.NgVideoOwnerUser.Any(x => x.UserId == userId))
				{
					return new NGResult()
					{
						NGReason = NGReason.UserId,
						NGDescription = "",
						Content = userId.ToString(),
					};
				}

				var titleNg = db.NgVideoTitleKeyword.FirstOrDefault(x => 0 != videoTitle.IndexOf(x.Keyword));
				if (titleNg != null)
				{
					return new NGResult()
					{
						NGReason = NGReason.Keyword,
						Content = titleNg.Keyword,
					};
				}

				var tagNg = db.NgVideoTag.FirstOrDefault(x => tags.Any(y => y.Value == x.Tag && (x.IgNgOnlyLock && y.Lock)));
				if (tagNg != null)
				{
					return new NGResult()
					{
						NGReason = NGReason.Tag,
						Content = tagNg.Tag + (tagNg.IgNgOnlyLock ? "(Lock)" : ""),
					};
				}

				return null;
			}
		}


		public static class VideoOwnerNg
		{
			public static IReadOnlyList<uint> GetNgVideoOwnerIds()
			{
				using (var db = new NgDbContext())
				{
					return db.NgVideoOwnerUser.Select(x => x.UserId).ToList();
				}
			}

			public static void AddNgVideoOwnerId(uint userId)
			{
				using (var db = new NgDbContext())
				{
					db.NgVideoOwnerUser.Add(new NgVideoOwnerUser()
					{
						UserId = userId
					});

					db.SaveChanges();
				}
			}

			public static void RemoveNgVideoOwnerId(uint userId)
			{
				using (var db = new NgDbContext())
				{
					var removeTarget = db.NgVideoOwnerUser.SingleOrDefault(x => x.UserId == userId);
					if (removeTarget != null)
					{
						db.NgVideoOwnerUser.Remove(removeTarget);
						db.SaveChanges();
					}
				}
			}
		}


		public static class VideoTitleNg
		{
			public static IReadOnlyList<string> GetNgVideoTitleList()
			{
				using (var db = new NgDbContext())
				{
					return db.NgVideoTitleKeyword.Select(x => x.Keyword).ToList();
				}
			}

			public static void AddNgVideoTitle(string keyword)
			{
				using (var db = new NgDbContext())
				{
					db.NgVideoTitleKeyword.Add(new NgVideoTitleKeyword()
					{
						Keyword = keyword
					});

					db.SaveChanges();
				}
			}

			public static void RemoveNgVideoTitle(string keyword)
			{
				using (var db = new NgDbContext())
				{
					var removeTarget = db.NgVideoTitleKeyword.SingleOrDefault(x => x.Keyword == keyword);
					if (removeTarget != null)
					{
						db.NgVideoTitleKeyword.Remove(removeTarget);
						db.SaveChanges();
					}
				}
			}
		}



		public static class VideoTagNg
		{
			public static IReadOnlyList<string> GetNgVideoTagList()
			{
				using (var db = new NgDbContext())
				{
					return db.NgVideoTag.Select(x => x.Tag).ToList();
				}
			}

			public static void AddNgVideoTag(string tag)
			{
				using (var db = new NgDbContext())
				{
					db.NgVideoTag.Add(new NgVideoTag()
					{
						Tag = tag
					});

					db.SaveChanges();
				}
			}

			public static void RemoveNgVideoTag(string tag)
			{
				using (var db = new NgDbContext())
				{
					var removeTarget = db.NgVideoTag.SingleOrDefault(x => x.Tag == tag);
					if (removeTarget != null)
					{
						db.NgVideoTag.Remove(removeTarget);
						db.SaveChanges();
					}
				}
			}
		}

	}

	
}
