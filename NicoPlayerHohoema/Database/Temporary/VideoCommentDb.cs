using Mntone.Nico2.Videos.Comment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace NicoPlayerHohoema.Database
{
    public static class VideoCommentDb
    {
        public static NicoVideoComment Get(string videoId)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            {
                return db.SingleOrDefault<NicoVideoComment>(x => x.VideoId == videoId);
            }
        }

        public static void AddOrUpdate(string videoId, IEnumerable<Chat> chatItems)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            {
                var comment = db.SingleOrDefault<NicoVideoComment>(x => x.VideoId == videoId);
                if (comment == null)
                {
                    comment = new NicoVideoComment
                    {
                        VideoId = videoId,
                    };
                }

                comment.ChatItems = chatItems.ToList();

                db.Upsert<NicoVideoComment>(comment);
            }
        }

        public static bool Remove(string videoId)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            {
                return db.Delete<NicoVideoComment>(x => x.VideoId == videoId) > 0;
            }
        }
    }


    public sealed class NicoVideoComment
    {
        [BsonId]
        public string VideoId{ get; set; }

        public List<Chat> ChatItems { get; set; }
    }
}
