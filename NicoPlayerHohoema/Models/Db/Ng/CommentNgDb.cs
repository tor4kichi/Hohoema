using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Db.Ng
{
	public static class CommentNgDb
	{
		public static NGResult IsNgComment(string comment, uint userId)
		{
			using (var db = new NgDbContext())
			{
				if (db.NgCommentUserId.Any(x => x.UserId == userId))
				{
					return new NGResult()
					{
						NGReason = NGReason.UserId,
						NGDescription = "",
						Content = userId.ToString(),
					};
				}

				var ngKeyword = db.NgCommentKeyword.FirstOrDefault(x => 0 != comment.IndexOf(x.Keyword));
				if (ngKeyword != null)
				{
					return new NGResult()
					{
						NGReason = NGReason.Keyword,
						Content = userId.ToString()
					};
				}

				return null;
			}
		}

		public static class CommentOwnerNg
		{
			public static IReadOnlyList<uint> GetNgCommentOwnerIds()
			{
				using (var db = new NgDbContext())
				{
					return db.NgCommentUserId.Select(x => x.UserId).ToList();
				}
			}

			public static void AddNgCommentOwner(uint userId)
			{
				using (var db = new NgDbContext())
				{
					db.NgCommentUserId.Add(new NgCommentUserId()
					{
						UserId = userId
					});

					db.SaveChanges();
				}
			}

			public static void RemoveNgCommentOwner(uint userId)
			{
				using (var db = new NgDbContext())
				{
					var removeTarget = db.NgCommentUserId.SingleOrDefault(x => x.UserId == userId);
					if (removeTarget != null)
					{
						db.NgCommentUserId.Remove(removeTarget);
						db.SaveChanges();
					}
				}
			}
		}

		public static class CommentKeywordNg
		{
			public static IReadOnlyList<string> GetNgCommentKeyword()
			{
				using (var db = new NgDbContext())
				{
					return db.NgCommentKeyword.Select(x => x.Keyword).ToList();
				}
			}

			public static void AddNgCommentKeyword(string keyword)
			{
				using (var db = new NgDbContext())
				{
					db.NgCommentKeyword.Add(new NgCommentKeyword()
					{
						Keyword = keyword
					});

					db.SaveChanges();
				}
			}

			public static void RemoveNgCommentKeyword(string keyword)
			{
				using (var db = new NgDbContext())
				{
					var removeTarget = db.NgCommentKeyword.SingleOrDefault(x => x.Keyword == keyword);
					if (removeTarget != null)
					{
						db.NgCommentKeyword.Remove(removeTarget);
						db.SaveChanges();
					}
				}
			}
		}
	}
}
