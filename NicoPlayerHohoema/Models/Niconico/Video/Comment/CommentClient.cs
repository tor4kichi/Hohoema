using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Dmc;
using NicoPlayerHohoema.Models.Db;
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

        private CommentSubmitInfo SubmitInfo { get; set; }


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
            var j = CommentDb.Get(RawVideoId);
            return j?.GetComments();
        }

        // コメントのキャッシュまたはオンラインからの取得と更新
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
                CommentDb.AddOrUpdate(RawVideoId, commentRes);
            }

            if (commentRes != null && SubmitInfo == null)
            {
                SubmitInfo = new CommentSubmitInfo();
                SubmitInfo.Ticket = commentRes.Thread.Ticket;
                if (int.TryParse(commentRes.Thread.CommentCount, out int count))
                {
                    SubmitInfo.CommentCount = count + 1;
                }
            }

            return commentRes?.Chat;


        }


        public bool CanGetCommentsFromNMSG 
        {
            get
            {
                return LastAccessDmcWatchResponse != null &&
                    LastAccessDmcWatchResponse.Video.DmcInfo != null;
            }
        }

        public async Task<NMSG_Response> GetCommentsFromNMSG()
        {
            if (LastAccessDmcWatchResponse == null) { return null; }

            var res = await Context.Video.GetNMSGCommentAsync(LastAccessDmcWatchResponse);

            if (res != null && SubmitInfo == null)
            {
                SubmitInfo = new CommentSubmitInfo();
                SubmitInfo.Ticket = res.Thread.Ticket;
                SubmitInfo.CommentCount = LastAccessDmcWatchResponse.Thread.CommentCount + 1;
            }

            return res;
        }

        public async Task<PostCommentResponse> SubmitComment(string comment, TimeSpan position, string commands)
        {
            if (CommentServerInfo == null) { return null; }
            if (SubmitInfo == null) { return null; }

            if (CommentServerInfo == null)
            {
                throw new Exception("コメント投稿には事前に動画ページへのアクセスとコメント情報取得が必要です");
            }

            PostCommentResponse response = null;
            foreach (var cnt in Enumerable.Range(0, 2))
            {
                try
                {
                    response = await Context.Video.PostCommentAsync(
                        CommentServerInfo.ServerUrl,
                        CommentServerInfo.DefaultThreadId.ToString(),
                        SubmitInfo.Ticket,
                        SubmitInfo.CommentCount,
                        comment,
                        position,
                        commands
                        );
                }
                catch
                {
                    // コメントデータを再取得してもう一度？
                    return null;
                }

                if (response.Chat_result.Status == ChatResult.Success)
                {
                    SubmitInfo.CommentCount++;
                    break;
                }

                Debug.WriteLine("コメ投稿失敗: コメ数 " + SubmitInfo.CommentCount);

                await Task.Delay(1000);

                try
                {
                    var videoInfo = await Context.Search.GetVideoInfoAsync(RawVideoId);
                    SubmitInfo.CommentCount = int.Parse(videoInfo.Thread.num_res);
                    Debug.WriteLine("コメ数再取得: " + SubmitInfo.CommentCount);
                }
                catch
                {
                }
            }

            return response;
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
                if (SubmitInfo == null) { return false; }

                return true;
            }
        }
        
    }
}
