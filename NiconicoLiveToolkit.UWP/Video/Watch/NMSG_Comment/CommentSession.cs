using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
#if WINDOWS_UWP
using Windows.Web.Http;
using Windows.Web.Http.Headers;
#else
using System.Net.Http;
using System.Net.Http.Headers;
#endif

namespace NiconicoToolkit.Video.Watch.NMSG_Comment
{
    public sealed class CommentSession
    {
       
        class CommentSessionContext
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
            private int _SubmitTimes = 0;
            private int _SeqNum = 0;

            public CommentSessionContext(NiconicoContext context)
            {
                _context = context;
            }

            void IncrementSequenceNumber(int incrementCount)
            {
                _SeqNum += incrementCount;
                _SubmitTimes += 1;
            }

            internal CommentSessionSendingCommandBuilder CreateNextCommandBuilder()
            {
                return new CommentSessionSendingCommandBuilder(_SubmitTimes, _SeqNum);
            }

            public async Task<T> SendCommentCommandAsync<T>(Uri server, CommentSessionSendingCommandBuilder dataBuilder)
            {
                var requestParamsJson = dataBuilder.GetSerializeJson();

                IncrementSequenceNumber(dataBuilder.IncrementCount);
                return await _context.SendJsonAsAsync<T>( HttpMethod.Post, 
                    $"{server.OriginalString}/api.json", requestParamsJson, _CommentCommandResponseJsonSerializerOptions);
            }

        }

        class CommentSessionSendingCommandBuilder
        {
            private static readonly JsonSerializerOptions _options = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            List<object> _commands;
            public int SubmitTime { get; }
            public int SeqNum { get; }
            public int IncrementCount { get; private set; }
            
            public CommentSessionSendingCommandBuilder(int submitTime, int seqNum)
            {
                SubmitTime = submitTime;
                SeqNum = seqNum;
                IncrementCount = 0;
                _commands = new List<object>();

                AddCommand_Internal(new PingItem($"rs:{SubmitTime}"));
            }

            public void AddCommand<CommandType>(CommandType command)
                where CommandType : ICommentSessionCommand_Sending
            {
                var currentSeqNum = SeqNum + IncrementCount;
                AddCommand_Internal(new PingItem($"ps:{currentSeqNum}"));
                AddCommand_Internal(command);
                AddCommand_Internal(new PingItem($"pf:{currentSeqNum}"));
                IncrementCount++;
            }

            public string GetSerializeJson()
            {
                AddCommand_Internal(new PingItem($"rf:{SubmitTime}"));
                return JsonSerializer.Serialize(_commands, _options);
            }

            private void AddCommand_Internal<CommandType>(CommandType command)
                where CommandType : ICommentSessionCommand
            {
                _commands.Add(command);
            }
        }

        private readonly NiconicoContext _context;
        private readonly DmcWatchApiData _dmcApiData;
        private Comment _comment => _dmcApiData.Comment;
        private Thread _defaultPostTargetThread => _comment.Threads.FirstOrDefault(x => x.IsDefaultPostTarget);

        private string _userId;
        private string _userKey;
        private bool _isPremium;

        private string _DefaultPostTargetThreadId;
        private string DefaultPostTargetThreadId => _DefaultPostTargetThreadId ?? (_DefaultPostTargetThreadId = _defaultPostTargetThread.Id.ToString());
        private string _ThreadLeavesContentString;
        private int _LastRes = 0;
        private string _Ticket = null;


        private readonly CommentSessionContext _commentSessionContext;

        public CommentSession(NiconicoContext context, DmcWatchApiData dmcApiData)
        {
            _context = context;
            _dmcApiData = dmcApiData;

            var userId = _dmcApiData.Viewer?.Id.ToString();
            _userId = userId == null || userId == "0" ? "" : userId;
            _userKey = _dmcApiData.Comment.Keys.UserKey;
            _isPremium = _dmcApiData.Viewer?.IsPremium ?? false;

            _ThreadLeavesContentString = ThreadLeaves.MakeContentString(TimeSpan.FromSeconds(dmcApiData.Video.Duration));

            _commentSessionContext = new CommentSessionContext(context);
        }

        public bool CanPostComment => _Ticket != null;


        

        public async Task<NMSG_Response> GetCommentFirstAsync()
        {
            var hasCommunityThread = _comment.Threads.Any(x => x.Label == "community");

            var builder = _commentSessionContext.CreateNextCommandBuilder();
            foreach (var thread in _comment.Threads)
            {
                if (!thread.IsActive)
                {
                    continue;
                }

                ThreadKeyResponse threadKey = null;
                if (thread.IsThreadkeyRequired)
                {
                    threadKey = await GetThreadKeyAsync(thread.Id);
                }

                if (thread.IsOwnerThread)
                {
                    builder.AddCommand(new ThreadItem()
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
                    builder.AddCommand(new ThreadItem()
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

                if (thread.IsLeafRequired)
                {
                    builder.AddCommand(new ThreadLeavesItem()
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
                }
            }

            // コメント取得リクエストを送信
            var res = await _commentSessionContext.SendCommentCommandAsync<NMSG_Response>(_defaultPostTargetThread.Server, builder);

            var defaultPostThreadInfo = res.Threads.FirstOrDefault(x => x.Thread == DefaultPostTargetThreadId);
            if (defaultPostThreadInfo != null)
            {
                _LastRes = defaultPostThreadInfo.LastRes;
                _Ticket = defaultPostThreadInfo.Ticket ?? _Ticket;
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

            var builder = _commentSessionContext.CreateNextCommandBuilder();

            builder.AddCommand(new ThreadItem()
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
            });

            // コメント取得リクエストを送信
            var res = await _commentSessionContext.SendCommentCommandAsync<NMSG_Response>(_defaultPostTargetThread.Server, builder);

            var defaultPostThreadInfo = res.Threads.FirstOrDefault(x => x.Thread == DefaultPostTargetThreadId);
            if (defaultPostThreadInfo != null)
            {
                _LastRes = defaultPostThreadInfo.LastRes;
                _Ticket = defaultPostThreadInfo.Ticket ?? _Ticket;
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

            var builder = _commentSessionContext.CreateNextCommandBuilder();
            builder.AddCommand(new PostChatData()
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
            });
            

            var res = await _commentSessionContext.SendCommentCommandAsync<PostCommentResponse>(_defaultPostTargetThread.Server, builder);

            if (res?.ChatResult.Status == ChatResultCode.Success)
            {
                _LastRes = (int)res.ChatResult.No;
            }

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
            using (var enumerater = document.RootElement.EnumerateArray())
            {
                foreach (var elem in enumerater)
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
