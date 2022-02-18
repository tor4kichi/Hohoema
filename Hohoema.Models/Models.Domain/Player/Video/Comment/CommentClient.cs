using NiconicoToolkit.Live.WatchSession;
using Hohoema.Models.Helpers;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NiconicoSession = Hohoema.Models.Domain.Niconico.NiconicoSession;
using NiconicoToolkit.Video.Watch;
using NiconicoToolkit.Video.Watch.NMSG_Comment;
using Hohoema.Models.Domain.Player.Comment;

namespace Hohoema.Models.Domain.Player.Video.Comment
{
    public class CommentClient
    {
        // コメントの取得、とアプリケーションドメインなコメントモデルへの変換
        // コメントの投稿可否判断と投稿処理

        public CommentClient(NiconicoSession niconicoSession, string rawVideoid)
        {
            _niconicoSession = niconicoSession;
            RawVideoId = rawVideoid;
        }

        public string RawVideoId { get; }
        public CommentServerInfo CommentServerInfo { get; set; }

        internal DmcWatchApiData DmcWatch { get; set; }


        private CommentSession _CommentSession;
        private readonly NiconicoSession _niconicoSession;
        private NiconicoToolkit.NiconicoContext _toolkitContext => _niconicoSession.ToolkitContext;

        private CommentSession CommentSession
        {
            get
            {
                return _CommentSession ?? (_CommentSession = DmcWatch != null ? new CommentSession(_toolkitContext, DmcWatch) : null);
            }
        }


        private bool CanGetCommentsFromNMSG 
        {
            get
            {
                return CommentSession != null;
            }
        }

        private async Task<NMSG_Response> GetCommentsFromNMSG()
        {
            if (CommentSession == null) { return null; }

            return await CommentSession.GetCommentFirstAsync();
        }

        public async Task<PostCommentResponse> SubmitComment(string comment, TimeSpan position, string commands)
        {
            return await CommentSession.PostCommentAsync(position, comment, commands);
        }

        public bool IsAllowAnnonimityComment
        {
            get
            {
                if (DmcWatch == null) { return false; }

                if (DmcWatch.Channel != null) { return false; }

                if (DmcWatch.Community != null) { return false; }

                return true;
            }
        }

        public bool CanSubmitComment
        {
            get
            {
                if (!Helpers.InternetConnection.IsInternet()) { return false; }

                if (CommentServerInfo == null) { return false; }

                return true;
            }
        }


        public async Task<IEnumerable<VideoComment>> GetCommentsAsync()
        {
            if (CanGetCommentsFromNMSG)
            {
                var res = await GetCommentsFromNMSG();
                return res.Comments.Select(x => ChatToComment(x));
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public string VideoOwnerId { get; set; }

        private VideoComment ChatToComment(NMSG_Chat rawComment)
        {
            return new VideoComment()
            {
                CommentText = rawComment.Content,
                CommentId = rawComment.No,
                VideoPosition = rawComment.Vpos.ToTimeSpan(),
                UserId = rawComment.UserId,
                Mail = rawComment.Mail,
                NGScore = rawComment.Score ?? 0,
                IsAnonymity = rawComment.Anonymity != 0,
                IsLoginUserComment = _niconicoSession.IsLoggedIn && rawComment.Anonymity == 0 && rawComment.UserId == _niconicoSession.UserId,
                //IsOwnerComment = rawComment.UserId != null && rawComment.UserId == VideoOwnerId,
                DeletedFlag = rawComment.Deleted ?? 0
            };
        }








    }
}
