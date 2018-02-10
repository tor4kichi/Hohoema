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

        private CommentSubmitInfo DefaultThreadSubmitInfo { get; set; }
        private CommentSubmitInfo CommunityThreadSubmitInfo { get; set; }


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
        // コメントのキャッシュまたはオンラインからの取得と更新
            return j?.GetComments();
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
                CommentDb.AddOrUpdate(RawVideoId, commentRes);
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
                return LastAccessDmcWatchResponse != null &&
                    LastAccessDmcWatchResponse.Video.DmcInfo != null;
            }
        }

        public async Task<NMSG_Response> GetCommentsFromNMSG()
        {
            if (LastAccessDmcWatchResponse == null) { return null; }

            var res = await Context.Video.GetNMSGCommentAsync(LastAccessDmcWatchResponse);

            if (res != null && DefaultThreadSubmitInfo == null)
            {
                DefaultThreadSubmitInfo = new CommentSubmitInfo();
                DefaultThreadSubmitInfo.Ticket = res.Threads.First(x => x.Thread == CommentServerInfo.DefaultThreadId.ToString()).Ticket;
                DefaultThreadSubmitInfo.CommentCount = LastAccessDmcWatchResponse.Thread.CommentCount + 1;

                if (CommentServerInfo.CommunityThreadId.HasValue)
                {
                    var communityThreadId = CommentServerInfo.CommunityThreadId.Value.ToString();
                    var communityThread = res.Threads.FirstOrDefault(x => x.Thread == communityThreadId);
                    if (communityThread != null)
                    {
                        CommunityThreadSubmitInfo = new CommentSubmitInfo()
                        {
                            Ticket = communityThread.Ticket,
                            CommentCount = communityThread.LastRes
                        };
                    }
                }
            }

            return res;
        }

        public async Task<PostCommentResponse> SubmitComment(string comment, TimeSpan position, string commands)
        {
            if (CommentServerInfo == null) { return null; }
            if (DefaultThreadSubmitInfo == null) { return null; }

            if (CommentServerInfo == null)
            {
                throw new Exception("コメント投稿には事前に動画ページへのアクセスとコメント情報取得が必要です");
            }

            PostCommentResponse response = null;

            // 視聴中にコメント数が増えていってコメントのblock_noが100毎の境界を超える場合に対応するため
            // 投稿の試行ごとにコメント投稿先Blockを飛ばすため、コメント数に 試行数 * 100 を加算しています
            if (CommentServerInfo.CommunityThreadId.HasValue)
            {
                Debug.WriteLine($"書き込み先:{CommentServerInfo.CommunityThreadId.Value} (community thread)");

                var threadId = CommentServerInfo.CommunityThreadId.Value.ToString();
                
                {
                    try
                    {
                        response = await Context.Video.NMSGPostCommentAsync(
                            //                        CommentServerInfo.ServerUrl,
                            threadId,
                            DefaultThreadSubmitInfo.Ticket,
                            CommunityThreadSubmitInfo.CommentCount,
                            CommentServerInfo.ViewerUserId,
                            comment,
                            position,
                            commands
                            );
                    }
                    catch
                    {
                        Debug.WriteLine("コメント投稿で致命的なエラー、投稿試行を中断します");
                        return null;
                    }
                }
            }
            
            if (response?.Chat_result.Status != ChatResult.Success)
            {
                Debug.WriteLine($"書き込み先:{CommentServerInfo.DefaultThreadId} (default thread)");

                var threadId = CommentServerInfo.DefaultThreadId.ToString();
                
                try
                {
                    response = await Context.Video.NMSGPostCommentAsync(
                        //                        CommentServerInfo.ServerUrl,
                        threadId,
                        DefaultThreadSubmitInfo.Ticket,
                        DefaultThreadSubmitInfo.CommentCount,
                        CommentServerInfo.ViewerUserId,
                        comment,
                        position,
                        commands
                        );
                }
                catch
                {
                    Debug.WriteLine("コメント投稿で致命的なエラー、投稿試行を中断します");
                    return null;
                }
            }

            Debug.WriteLine("コメント投稿結果： " + response.Chat_result.Status);

            if (response?.Chat_result.Status == ChatResult.Success)
            {
                DefaultThreadSubmitInfo.CommentCount = response?.Chat_result.No ?? DefaultThreadSubmitInfo.CommentCount;

                Debug.WriteLine("投稿後のコメント数: " + DefaultThreadSubmitInfo.CommentCount);
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
                if (DefaultThreadSubmitInfo == null) { return false; }

                return true;
            }
        }
        
    }
}
