using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Dmc;
using NiconicoLiveToolkit.Live.WatchSession;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Models.Niconico;
using NicoPlayerHohoema.Models.Niconico.Video;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
    public class CommentClient : Provider.ProviderBase
    {
        // コメントの取得、とアプリケーションドメインなコメントモデルへの変換
        // コメントの投稿可否判断と投稿処理

        public CommentClient(NiconicoSession niconicoSession, string rawVideoid)
            : base(niconicoSession)
        {
            RawVideoId = rawVideoid;
        }

        public string RawVideoId { get; }
        public CommentServerInfo CommentServerInfo { get; set; }

        private CommentResponse CachedCommentResponse { get; set; }

        internal DmcWatchResponse DmcWatch { get; set; }

        private CommentSubmitInfo DefaultThreadSubmitInfo { get; set; }
        private CommentSubmitInfo CommunityThreadSubmitInfo { get; set; }


        private CommentSessionContext _CommentSessionContext;
        public CommentSessionContext CommentSessionContext
        {
            get
            {
                return _CommentSessionContext ?? (_CommentSessionContext = DmcWatch != null ? NiconicoSession.Context.Video.GetCommentSessionContext(DmcWatch) : null);
            }
        }

        private async Task<List<Chat>> GetComments()
        {
            if (CommentServerInfo == null) { return new List<Chat>(); }

            CommentResponse commentRes = null;
            try
            {
                
                commentRes = await ConnectionRetryUtil.TaskWithRetry(async () =>
                {
                    return await ContextActionAsync(async context => 
                    {
                        return await context.Video
                            .GetCommentAsync(
                                (int)CommentServerInfo.ViewerUserId,
                                CommentServerInfo.ServerUrl,
                                CommentServerInfo.DefaultThreadId,
                                CommentServerInfo.ThreadKeyRequired
                            );
                    });
                    
                });

            }
            catch
            {
                
            }


            if (commentRes?.Chat.Count == 0)
            {
                try
                {
                    if (CommentServerInfo.CommunityThreadId.HasValue)
                    {
                        commentRes = await ConnectionRetryUtil.TaskWithRetry(async () =>
                        {
                            return await ContextActionAsync(async context =>
                            {
                                return await context.Video
                                .GetCommentAsync(
                                    (int)CommentServerInfo.ViewerUserId,
                                    CommentServerInfo.ServerUrl,
                                    CommentServerInfo.CommunityThreadId.Value,
                                    CommentServerInfo.ThreadKeyRequired
                                );
                            });
                        });
                    }
                }
                catch { }
            }

            if (commentRes != null)
            {
                CachedCommentResponse = commentRes;
            }

            if (commentRes != null && DefaultThreadSubmitInfo == null)
            {
                DefaultThreadSubmitInfo = new CommentSubmitInfo();
                DefaultThreadSubmitInfo.Ticket = commentRes.Thread.Ticket;
                if (int.TryParse(commentRes.Thread.CommentCount, out int count))
                {
                    DefaultThreadSubmitInfo.CommentCount = count + 1;
                }
            }

            return commentRes?.Chat;
        }


        private bool CanGetCommentsFromNMSG 
        {
            get
            {
                return CommentSessionContext != null;
            }
        }

        private async Task<NMSG_Response> GetCommentsFromNMSG()
        {
            if (CommentSessionContext == null) { return null; }

            return await CommentSessionContext.GetCommentFirstAsync();
        }

        public async Task<PostCommentResponse> SubmitComment(string comment, TimeSpan position, string commands)
        {
            return await CommentSessionContext.PostCommentAsync(position, comment, commands);
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
                if (DefaultThreadSubmitInfo == null) { return false; }

                return true;
            }
        }


        public async Task<List<Comment>> GetCommentsAsync()
        {
            List<Comment> comments = null;

            if (CanGetCommentsFromNMSG)
            {
                try
                {
                    var res = await GetCommentsFromNMSG();

                    var rawComments = res.ParseComments();

                    comments = rawComments.Select(x => ChatToComment(x)).ToList();
                }
                catch
                {
                }
            }

            // 新コメサーバーがダメだったら旧サーバーから取得
            if (comments == null)
            {
                List<Chat> oldFormatComments = null;
                try
                {
                    oldFormatComments = await GetComments();
                }
                catch
                {
                    return new List<Comment>();
                }

                comments = oldFormatComments?.Select(x => ChatToComment(x)).ToList();
            }

            return comments ?? new List<Comment>();
        }

        public string VideoOwnerId { get; set; }

        private Comment ChatToComment(Chat rawComment)
        {
            return new Comment()
            {
                CommentText = rawComment.Text,
                CommentId = rawComment.GetCommentNo(),
                VideoPosition = rawComment.GetVpos().ToTimeSpan(),
                UserId = rawComment.UserId,
                Mail = rawComment.Mail,
                NGScore = 0,
                IsAnonymity = rawComment.GetAnonymity(),
                IsLoginUserComment = NiconicoSession.IsLoggedIn && rawComment.UserId == NiconicoSession.UserIdString,
                IsOwnerComment = rawComment.UserId != null && rawComment.UserId == VideoOwnerId,
            };
        }

        private Comment ChatToComment(NMSG_Chat rawComment)
        {
            return new Comment()
            {
                CommentText = rawComment.Content,
                CommentId = (uint)rawComment.No,
                VideoPosition = rawComment.Vpos.ToTimeSpan(),
                UserId = rawComment.UserId,
                Mail = rawComment.Mail,
                NGScore = rawComment.Score ?? 0,
                IsAnonymity = rawComment.Anonymity != 0,
                IsLoginUserComment = NiconicoSession.IsLoggedIn && rawComment.UserId == NiconicoSession.UserIdString,
                IsOwnerComment = rawComment.UserId != null && rawComment.UserId == VideoOwnerId,
                DeletedFlag = rawComment.Deleted ?? 0
            };
        }








    }
}
