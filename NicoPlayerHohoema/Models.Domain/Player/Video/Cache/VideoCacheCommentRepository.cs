using MonkeyCache;
using Newtonsoft.Json;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Player.Video.Comment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Player.Video.Cache
{
    // ICommentSessionとは無関係にコメントのキャッシュだけ管理する

    public class VideoCacheCommentRepository
    {
        private readonly IBarrel _barrel;

        public VideoCacheCommentRepository(IBarrel barrel)
        {
            _barrel = barrel;
        }


        public bool IsExpired(string videoId)
        {
            return _barrel.IsExpired(videoId + "_c");
        }

        public CommentEntity GetCached(string videoId)
        {
            var commentDbId = videoId + "_c";
            return _barrel.Get<CommentEntity>(commentDbId);
        }

        public void SetCache(string videoId, IEnumerable<VideoComment> comments)
        {
            var entity = new CommentEntity() 
            {
                ContentId = videoId,
                Comments = comments.ToArray()
            };
            var commentDbId = videoId + "_c";
            _barrel.Add(commentDbId, entity, TimeSpan.FromDays(1));
        }
    }

    [DataContract]
    public class CommentEntity
    {
        [DataMember]
        public string ContentId { get; internal set; }

        [DataMember]
        public VideoComment[] Comments { get; internal set; }
    }
}
