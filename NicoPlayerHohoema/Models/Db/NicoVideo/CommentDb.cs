using Mntone.Nico2.Videos.Comment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Db
{
	public static class CommentDb
	{
		public static void AddOrUpdate(string threadId, CommentResponse commentRes)
		{
			using (var db = new NicoVideoDbContext())
			{
				var comment = db.Comments.SingleOrDefault(x => x.ThreadId == threadId);

				if (comment == null)
				{
					comment = new NicoVideoComment()
					{
						ThreadId = threadId,
						CommentCount = commentRes.GetCommentCount()
					};

					db.Add(comment);
				}
				else
				{
					comment.CommentCount = commentRes.GetCommentCount();
					comment.SetComments(commentRes.Chat);
				}

				db.SaveChanges();
			}
		}


		public static void Remove(string threadId)
		{
			using (var db = new NicoVideoDbContext())
			{
				var comment = db.Comments.SingleOrDefault(x => x.ThreadId == threadId);

				db.Remove(comment);
				db.SaveChanges();
			}
		}
	}
}
