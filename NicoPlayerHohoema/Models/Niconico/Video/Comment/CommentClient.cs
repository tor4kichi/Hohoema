using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Dmc;
using NicoPlayerHohoema.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
    public class CommentClient
    {
        public string RawVideoId { get; }
        public NiconicoContext Context { get; }
        public CommentServerInfo CommentServerInfo { get; private set; }

        private CommentResponse CachedCommentResponse { get; set; }

        internal DmcWatchResponse LastAccessDmcWatchResponse { get; set; }

        private CommentSubmitInfo DefaultThreadSubmitInfo { get; set; }
        private CommentSubmitInfo CommunityThreadSubmitInfo { get; set; }


        private CommentSessionContext _CommentSessionContext;
        public CommentSessionContext CommentSessionContext
        {
            get
            {
                return _CommentSessionContext ?? (_CommentSessionContext = LastAccessDmcWatchResponse != null ? Context.Video.GetCommentSessionContext(LastAccessDmcWatchResponse) : null);
            }
        }


        public CommentClient(NiconicoContext context, string rawVideoid)
        {
            RawVideoId = rawVideoid;
            Context = context;
        }

        public CommentClient(NiconicoContext context, CommentServerInfo serverInfo)
        {
            RawVideoId = serverInfo.VideoId;
            Context = context;
            CommentServerInfo = serverInfo;
        }

        public List<Chat> GetCommentsFromLocal()
        {
            var j = Database.VideoCommentDb.Get(RawVideoId);
        // コメントのキャッシュまたはオンラインからの取得と更新
            return j?.ChatItems;
        }

        public async Task<List<Chat>> GetComments()
        {
            if (CommentServerInfo == null) { return new List<Chat>(); }

            CommentResponse commentRes = null;
            try
            {
                
                commentRes = await ConnectionRetryUtil.TaskWithRetry(async () =>
                {
                    return await this.Context.Video
                        .GetCommentAsync(
                            (int)CommentServerInfo.ViewerUserId,
                            CommentServerInfo.ServerUrl,
                            CommentServerInfo.DefaultThreadId,
                            CommentServerInfo.ThreadKeyRequired
                        );
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
                            return await Context.Video
                                .GetCommentAsync(
                                    (int)CommentServerInfo.ViewerUserId,
                                    CommentServerInfo.ServerUrl,
                                    CommentServerInfo.CommunityThreadId.Value,
                                    CommentServerInfo.ThreadKeyRequired
                                );
                        });
                    }
                }
                catch { }
            }

            if (commentRes != null)
            {
                CachedCommentResponse = commentRes;
                Database.VideoCommentDb.AddOrUpdate(RawVideoId, commentRes.Chat);
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


        public bool CanGetCommentsFromNMSG 
        {
            get
            {
                return CommentSessionContext != null;
            }
        }

        public async Task<NMSG_Response> GetCommentsFromNMSG()
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
                if (LastAccessDmcWatchResponse == null) { return false; }

                if (LastAccessDmcWatchResponse.Channel != null) { return false; }

                if (LastAccessDmcWatchResponse.Community != null) { return false; }

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
        
    }
}
