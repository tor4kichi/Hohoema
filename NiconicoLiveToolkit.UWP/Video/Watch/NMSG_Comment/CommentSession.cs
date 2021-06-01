using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Windows.Web.Http;

namespace NiconicoToolkit.Video.Watch.NMSG_Comment
{
    public sealed class CommentSession
    {
        private readonly JsonSerializerOptions _CommentCommandResponseJsonSerializerOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new NMSG_ResponseConverter(),
                new NMSG_ChatResultConverter(),
                new LongToStringConverter(),
            }
        };


        private readonly NiconicoContext _context;
        private readonly DmcWatchApiData _dmcApiData;
        private Comment _comment => _dmcApiData.Comment;
        private Thread _defaultPostTargetThread => _comment.Threads.FirstOrDefault(x => x.IsDefaultPostTarget);

        private string _userId;
        private string _userKey;
        private bool _isPremium;

        private string _DefaultPostTargetThreadId;
        private string DefaultPostTargetThreadId => _DefaultPostTargetThreadId ?? (_DefaultPostTargetThreadId = _defaultPostTargetThread.Id.ToString());
        private int _SubmitTimes = 0;
        private int _SeqNum = 0;
        private string _ThreadLeavesContentString;
        private int _LastRes = 0;
        private string _Ticket = null;

        public CommentSession(NiconicoContext context, DmcWatchApiData dmcApiData)
        {
            _context = context;
            _dmcApiData = dmcApiData;

            var userId = _dmcApiData.Viewer?.Id.ToString();
            _userId = userId == null || userId == "0" ? "" : userId;
            _userKey = _dmcApiData.Comment.Keys.UserKey;
            _isPremium = _dmcApiData.Viewer?.IsPremium ?? false;

            _ThreadLeavesContentString = ThreadLeaves.MakeContentString(TimeSpan.FromSeconds(dmcApiData.Video.Duration));
        }

        public bool CanPostComment => _Ticket != null;


        private void IncrementSequenceNumber(int incrementCount)
        {
            _SeqNum += incrementCount;
            _SubmitTimes += 1;
        }

        public async Task<NMSG_Response> GetCommentFirstAsync()
        {
            var hasCommunityThread = _comment.Threads.Any(x => x.Label == "community");

            List<object> commentCommandList = new List<object>();

            commentCommandList.Add(new PingItem($"rs:{_SubmitTimes}"));
            var seqNum = _SeqNum;
            foreach (var thread in _comment.Threads)
            {
                if (!thread.IsActive)
                {
                    continue;
                }

                commentCommandList.Add(new PingItem($"ps:{_SeqNum + seqNum}"));

                ThreadKeyResponse threadKey = null;
                if (thread.IsThreadkeyRequired)
                {
                    threadKey = await GetThreadKeyAsync(thread.Id);
                }

                if (thread.IsOwnerThread)
                {
                    commentCommandList.Add(new ThreadItem()
                    {
                        Thread = new Thread_CommentRequest()
                        {
                            Fork = thread.Fork,
                            UserId = _userId,
                            ThreadId = thread.Id.ToString(),
                            Version = "20061206",
                            Userkey = _userKey,

                            ResFrom = -1000,
                        }
                    });
                }
                else
                {
                    commentCommandList.Add(new ThreadItem()
                    {
                        Thread = new Thread_CommentRequest()
                        {
                            Fork = thread.Fork,
                            UserId = _userId,
                            ThreadId = thread.Id.ToString(),
                            Version = "20090904",
                            Userkey = !thread.IsThreadkeyRequired ? _userKey : null,

                            Threadkey = threadKey?.ThreadKey,
                            Force184 = threadKey?.Force184,
                        }
                    });
                }


                commentCommandList.Add(new PingItem($"pf:{_SeqNum + seqNum}"));

                ++seqNum;

                if (thread.IsLeafRequired)
                {
                    commentCommandList.Add(new PingItem($"ps:{_SeqNum + seqNum}"));

                    commentCommandList.Add(new ThreadLeavesItem()
                    {
                        ThreadLeaves = new ThreadLeaves()
                        {
                            Fork = thread.Fork,
                            UserId = _userId,
                            ThreadId = thread.Id.ToString(),
                            Userkey = !thread.IsThreadkeyRequired ? _userKey : null,

                            Threadkey = threadKey?.ThreadKey,
                            Force184 = threadKey?.Force184,

                            Content = _ThreadLeavesContentString,
                        }
                    });

                    commentCommandList.Add(new PingItem($"pf:{_SeqNum + seqNum}"));

                    ++seqNum;
                }
            }

            commentCommandList.Add(new PingItem($"rf:{_SubmitTimes}"));


            // コメント取得リクエストを送信
            var res = await SendCommentCommandAsync<NMSG_Response>(_defaultPostTargetThread.Server, commentCommandList.ToArray());

            var defaultPostThreadInfo = res.Threads.FirstOrDefault(x => x.Thread == DefaultPostTargetThreadId);
            if (defaultPostThreadInfo != null)
            {
                _LastRes = defaultPostThreadInfo.LastRes;
                _Ticket = defaultPostThreadInfo.Ticket ?? _Ticket;

                IncrementSequenceNumber(commentCommandList.Count);
            }

            return res;
        }


        /// <summary>
        /// 動画のコメント取得。コメント投稿後に差分を取得する際に使用する。
        /// </summary>
        /// <returns></returns>
        public async Task<NMSG_Response> GetDifferenceCommentAsync()
        {
            ThreadKeyResponse threadKey = null;
            if (_defaultPostTargetThread.IsThreadkeyRequired)
            {
                threadKey = await GetThreadKeyAsync(_defaultPostTargetThread.Id);
            }

            object[] commentCommandList = new object[]
            {
                new PingItem($"rs:{_SubmitTimes}"),
                new PingItem($"ps:{_SeqNum}"),
                new ThreadItem()
                {
                    Thread = new Thread_CommentRequest()
                    {
                        Fork = _defaultPostTargetThread.Fork,
                        UserId = _userId,
                        ThreadId = DefaultPostTargetThreadId,
                        Version = "20061206",
                        Userkey = _userKey,
                        ResFrom = _LastRes,

                        Threadkey = threadKey?.ThreadKey,
                        Force184 = threadKey?.Force184,
                    }
                },
                new PingItem($"pf:{_SeqNum}"),
                new PingItem($"rf:{_SubmitTimes}"),
            };

            // コメント取得リクエストを送信
            var res = await SendCommentCommandAsync<NMSG_Response>(_defaultPostTargetThread.Server, commentCommandList);

            var defaultPostThreadInfo = res.Threads.FirstOrDefault(x => x.Thread == DefaultPostTargetThreadId);
            if (defaultPostThreadInfo != null)
            {
                _LastRes = defaultPostThreadInfo.LastRes;
                _Ticket = defaultPostThreadInfo.Ticket ?? _Ticket;

                IncrementSequenceNumber(commentCommandList.Length);
            }

            return res;
        }


        internal async Task<ThreadKeyResponse> GetThreadKeyAsync(int threadId)
        {
            var url = $"https://flapi.nicovideo.jp/api/getthreadkey?thread={threadId}";
            var keyValuesString = await _context.GetStringAsync(url);
            var nvc = HttpUtility.ParseQueryString(keyValuesString);

            return new(nvc["threadkey"], nvc["force_184"]);
        }

        private async Task<T> SendCommentCommandAsync<T>(Uri server, object[] parameter)
        {
            var requestParamsJson = JsonSerializer.Serialize(parameter, new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

#if WINDOWS_UWP
            HttpStringContent content = new HttpStringContent(requestParamsJson);
#else
#endif

            return await _context.PostJsonAsAsync<T>(
                $"{server.OriginalString}/api.json", content, _CommentCommandResponseJsonSerializerOptions);
        }



        #region Post Comment


        private async Task<PostCommentResponse> PostCommentAsync_Internal(TimeSpan posision, string comment, string mail, string ticket, string postKey)
        {
            if (_defaultPostTargetThread == null)
            {
                throw new NotSupportedException("not found default comment post target.");
            }

            if (_defaultPostTargetThread.Is184Forced)
            {
                mail = mail.Replace("184", "");
            }

            var vpos = (int)(posision.TotalMilliseconds * 0.1);
            object[] commentCommandList = new object[]
            {
                new PingItem($"rs:{_SubmitTimes}"),
                new PingItem($"ps:{_SeqNum}"),
                new PostChatData()
                {
                    Chat = new PostChat()
                    {
                        ThreadId = DefaultPostTargetThreadId,
                        Vpos = vpos,
                        Mail = mail,
                        Ticket = _Ticket,
                        UserId = _userId,
                        Content = comment,
                        PostKey = postKey,
//                        Premium = _isPremium ? "1" : "0"
                    }
                },
                new PingItem($"pf:{_SeqNum}"),
                new PingItem($"rf:{_SubmitTimes}"),
            };

            var res = await SendCommentCommandAsync<PostCommentResponse>(_defaultPostTargetThread.Server, commentCommandList);

            if (res?.ChatResult.Status == ChatResultCode.Success)
            {
                _LastRes = (int)res.ChatResult.No;
            }

            IncrementSequenceNumber(commentCommandList.Length);

            return res;
        }


        /// <summary>
        /// コメント投稿。登校前に<see cref="GetCommentFirstAsync"/>、または<see cref="GetDifferenceCommentAsync"/>を呼び出して投稿のためのTicketを取得しておく必要があります。
        /// ログインしていない場合はコメント投稿は出来ません。
        /// </summary>
        /// <param name="position"></param>
        /// <param name="comment"></param>
        /// <param name="mail"></param>
        /// <returns></returns>
        public async Task<PostCommentResponse> PostCommentAsync(TimeSpan position, string comment, string mail)
        {
            if (_Ticket == null)
            {
                throw new Exception("not found posting ticket. once call GetCommentFirstAsync() then filling ticket in CommentSessionContext class inside.");
            }

            if (_postKey is null)
            {
                _postKey = await GetPostKeyAsync(DefaultPostTargetThreadId, _LastRes);
            }

            var res = await PostCommentAsync_Internal(position, comment, mail, _Ticket, _postKey);

            if (res.ChatResult.Status is ChatResultCode.InvalidPostkey or ChatResultCode.InvalidTichet)
            {
                // 最新のコメント数とチケットを取得
                await GetDifferenceCommentAsync();

                // ポストキーを再取得
                _postKey = await GetPostKeyAsync(DefaultPostTargetThreadId, _LastRes, forceRefresh: true);

                res = await PostCommentAsync_Internal(position, comment, mail, _Ticket, _postKey);
            }

            return res;
        }

        string _postKey;



        private async Task<string> GetPostKeyAsync(string threadId, int commentCount, bool forceRefresh = false)
        {
            var dict = new NameValueCollection();
            dict.Add("thread", threadId);
            dict.Add("block_no", (commentCount / 100).ToString());
            dict.Add("device", "1");
            dict.Add("version", "1");
            dict.Add("version_sub", "6");
            var url = new StringBuilder("https://flapi.nicovideo.jp/api/getpostkey")
                .AppendQueryString(dict)
                .ToString();
            var resString = await _context.GetStringAsync(url);
            var nvc = HttpUtility.ParseQueryString(resString);
            return nvc["postkey"];
        }

        #endregion Post Comment
    }

    internal sealed class NMSG_ResponseConverter : JsonConverter<NMSG_Response>
    {
        private static readonly byte[] s_chatUtf8 = Encoding.UTF8.GetBytes("chat");
        private static readonly byte[] s_leafUtf8 = Encoding.UTF8.GetBytes("leaf");
        private static readonly byte[] s_threadUtf8 = Encoding.UTF8.GetBytes("thread");
        private static readonly byte[] s_globalNumResUtf8 = Encoding.UTF8.GetBytes("global_num_res");

        public override NMSG_Response Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var threads = new List<NGMS_Thread_Response>();
            var comments = new List<NMSG_Chat>();
            var leaves = new List<ThreadLeaf>();
            var globalNumRes = default(NGMS_GlobalNumRes);
            using (JsonDocument document = JsonDocument.ParseValue(ref reader))
            {
                foreach (var elem in document.RootElement.EnumerateArray())
                {
                    if (elem.TryGetProperty(s_chatUtf8, out var chatJe))
                    {
                        comments.Add(chatJe.ToObject<NMSG_Chat>());
                    }
                    else if (elem.TryGetProperty(s_leafUtf8, out var leafJe))
                    {
                        leaves.Add(leafJe.ToObject<ThreadLeaf>());
                    }
                    else if (elem.TryGetProperty(s_threadUtf8, out var threadJe))
                    {
                        threads.Add(threadJe.ToObject<NGMS_Thread_Response>());
                    }
                    else if (elem.TryGetProperty(s_globalNumResUtf8, out var globalNumResJe))
                    {
                        globalNumRes = globalNumResJe.ToObject<NGMS_GlobalNumRes>();
                    }
                }
            }

            return new NMSG_Response() 
            {
                Comments = comments.ToArray(),
                GlobalNumRes = globalNumRes,
                Threads = threads.ToArray(),
                Leaves = leaves.ToArray(),
                //ThreadType = ThreadType.
            };
        }

        public override void Write(Utf8JsonWriter writer, NMSG_Response value, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }
    }


    internal sealed class NMSG_ChatResultConverter : JsonConverter<PostCommentResponse>
    {
        private static readonly byte[] s_chatResultUtf8 = Encoding.UTF8.GetBytes("chat_result");
        public override PostCommentResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument document = JsonDocument.ParseValue(ref reader))
            {
                var length = document.RootElement.GetArrayLength();
                for (int i = 0; i < length; i++)
                {
                    var elem = document.RootElement[i];
                    if (elem.TryGetProperty(s_chatResultUtf8, out var chatResultJe))
                    {
                        return new PostCommentResponse() { ChatResult = chatResultJe.ToObject<ChatResult>(options) };
                    }
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, PostCommentResponse value, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }
    }
}
