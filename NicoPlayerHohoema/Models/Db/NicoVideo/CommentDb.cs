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
        public static NicoVideoComment Get(string rawVideoId)
        {
            using (var db = new NicoVideoDbContext())
            {
                return  db.Comments.SingleOrDefault(x => x.ThreadId == rawVideoId);
            }
        }

		public static void AddOrUpdate(string rawVideoId, CommentResponse commentRes)
		{
			using (var db = new NicoVideoDbContext())
			{
				var comment = db.Comments.SingleOrDefault(x => x.ThreadId == rawVideoId);

				if (comment == null)
				{
					comment = new NicoVideoComment()
					{
						ThreadId = rawVideoId,
						CommentCount = commentRes.GetCommentCount()
					};

					db.Comments.Add(comment);
				}
				else
				{
					comment.CommentCount = commentRes.GetCommentCount();
					comment.SetComments(commentRes.Chat);

					db.Comments.Update(comment);
				}

				db.SaveChanges();
			}
		}


		public static void Remove(string rawVideoId)
		{
			using (var db = new NicoVideoDbContext())
			{
				var comment = db.Comments.SingleOrDefault(x => x.ThreadId == rawVideoId);
				if (comment != null)
				{
					db.Comments.Remove(comment);
					db.SaveChanges();
				}
			}
		}
	}
}
