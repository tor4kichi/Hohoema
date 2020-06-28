using MonkeyCache;
using Newtonsoft.Json;
using Hohoema.Models.Niconico;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Repository.NicoVideo
{
    // ICommentSessionとは無関係にコメントのキャッシュだけ管理する

    public class CommentRepository
    {
        private readonly IBarrel _barrel;

        public CommentRepository(IBarrel barrel)
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

        public void SetCache(string videoId, IEnumerable<Comment> comments)
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
        public Comment[] Comments { get; internal set; }
    }
}
