using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NiconicoToolkit.Live.WatchSession;
using NiconicoToolkit.Live.WatchSession.ToClientMessage;
using NiconicoToolkit.Live.WatchSession.Events;

namespace NiconicoToolkit.Live.WatchSession
{
    using AsyncLock = NeoSmart.AsyncLock.AsyncLock;
    using WebSocket = WebSocket4Net.WebSocket;

    public static class NiwavidedNicoLiveMessageHelper
    {
        public static string MakeNiwavidedMessage(int messageCount, string content)
        {
            var rs = messageCount;
            var ps = messageCount * 5;
            var pf = messageCount * 5;
            var rf = messageCount;
            return $"[{{\"ping\":{{\"content\":\"rs:{rs}\"}}}},{{\"ping\":{{\"content\":\"ps:{ps}\"}}}},{content},{{\"ping\":{{\"content\":\"pf:{pf}\"}}}},{{\"ping\":{{\"content\":\"rf:{rf}\"}}}}]";
        }
    }


    public sealed class LiveCommentSession : IDisposable
    {
        public static LiveCommentSession CreateForLiveStream(string messageServerUrl, string threadId, string userId, string userAgent)
        {
            return new LiveCommentSession(messageServerUrl, threadId, userId, userAgent);
        }

        public static LiveCommentSession CreateForTimeshift(string messageServerUrl, string threadId, string userId, string userAgent, string waybackkey, DateTimeOffset startTime)
        {
            return new LiveCommentSession(messageServerUrl, threadId, userId, userAgent, waybackkey, startTime);
        }


        public event EventHandler<CommentPostedEventArgs> CommentPosted;
        public event EventHandler<CommentReceivedEventArgs> CommentReceived;
        public event EventHandler<CommentServerConnectedEventArgs> Connected;
        public event EventHandler<CommentServerDisconnectedEventArgs> Disconnected;



        const uint FirstGetRecentMessageCount = 50;

        private readonly WebSocket _ws;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly AsyncLock _CommentSessionLock = new AsyncLock();

        public string MessageServerUrl { get; }
        public string UserId { get; }
        public string ThreadId { get; }

        private string _ticket;
        private int _MessageSendCount = 0;

        public bool IsTimeshift { get; }
        private int _LastRes;
        private readonly string _waybackkey;
        private readonly DateTimeOffset _startTime;

        private LiveCommentSession(string messageServerUrl, string threadId, string userId, string userAgent, string waybackkey, DateTimeOffset startTime)
            : this(messageServerUrl, threadId, userId, userAgent)
        {
            IsTimeshift = true;
            _waybackkey = waybackkey;
            _startTime = startTime;
        }


        private LiveCommentSession(string messageServerUrl, string threadId, string userId, string userAgent)
        {
            MessageServerUrl = messageServerUrl;
            ThreadId = threadId;
            UserId = userId;

            _ws = new WebSocket(
                messageServerUrl,
                "msg.nicovideo.jp#json",
                userAgent: userAgent,
                customHeaderItems: new Dictionary<string, string>()
                {
                    { "Pragma", "not-cache" },
                    { "Sec-WebSocket-Extensions", "permessage-deflate,client_max_window_bits" },
                    
                }.ToList()
                );

            _ws.MessageReceived += _ws_MessageReceived;
            _ws.Opened += _ws_Opened;
            _ws.Closed += _ws_Closed;
            _ws.Error += _ws_Error;
            _ws.EnableAutoSendPing = false;


            _jsonSerializerOptions = new JsonSerializerOptions()
            {
                Converters =
                {
                    new JsonStringEnumMemberConverter(),
                    new CommentSessionToClientMessageJsonConverter(),
                    new VideoPositionToTimeSpanConverter(),
                }
            };
        }

        private void _ws_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Debug.WriteLine($"[CommentSession] oucur error. ");
            Debug.WriteLine(e.Exception.ToString());
        }

        private void _ws_Opened(object sender, EventArgs e)
        {
            Debug.WriteLine($"[CommentSession] Opened.");
        }

        private void _ws_Closed(object sender, EventArgs e)
        {
            Debug.WriteLine($"[CommentSession] Closed.");
        }

        private void _ws_MessageReceived(object sender, WebSocket4Net.MessageReceivedEventArgs e)
        {
            //Debug.WriteLine($"[CommentSession] Message Received.");
            //Debug.WriteLine(e.Message);

            var message = JsonSerializer.Deserialize<CommentSessionToClientMessage>(e.Message, _jsonSerializerOptions);
            bool _ = message switch
            {
                Chat_CommentSessionToClientMessage chat => ProcessChatMessage(chat),
                //ChatResult_CommentSessionToClientMessage chatResult => ProcessChatResultMessage(chatResult),
                Thread_CommentSessionToClientMessage thread => ProcessThreadMessage(thread),
                Ping_CommentSessionToClientMessage ping => ProcessPingMessage(ping),
                _ => true
            };
        }

        #region Process Message

        private bool ProcessChatMessage(Chat_CommentSessionToClientMessage chat)
        {
            CommentReceived?.Invoke(this, new CommentReceivedEventArgs()
            {
                Chat = new LiveChatData()
                {
                    Thread = chat.Thread.ToString(),
                    CommentId = chat.CommentId,
                    Content = chat.Content,
                    Date = chat.Date,
                    DateUsec = chat.DateUsec,
                    IsAnonymity = chat.Anonymity == 1,
                    __Premium = chat.Premium,
                    IsYourPost = chat.Yourpost == 1,
                    Mail = chat.Mail,
                    Score = chat.Score,
                    UserId = chat.UserId,
                    VideoPosition = chat.VideoPosition
                }
            });
            return true;
        }

        

        private bool ProcessChatResultMessage(ChatResult_CommentSessionToClientMessage chatResult)
        {
            CommentPosted?.Invoke(this, new CommentPostedEventArgs() 
            {
                ChatResult = chatResult.Status,
                Thread = chatResult.Thread,
                No = chatResult.CommentId ?? -1
            });
            return true;
        }

        private bool ProcessThreadMessage(Thread_CommentSessionToClientMessage thread)
        {
            _ticket = thread.Ticket;
            _LastRes = thread.LastRes ?? 0;

            StartHeartbeatTimer();

            Connected?.Invoke(this, new CommentServerConnectedEventArgs()
            {
                LastRes = thread.LastRes ?? 0,
                Resultcode = thread.Resultcode,
                Revision = thread.Revision,
                ServerTime = thread.ServerTime,
                Thread = thread.Thread.ToString(),
                Ticket = thread.Ticket,
            });

            return true;
        }

        private bool ProcessPingMessage(Ping_CommentSessionToClientMessage ping)
        {
            if (ping.Content.StartsWith("pf"))
            {
                _MessageSendCount += 1;
            }

            return true;
        }

        #endregion



        public void Dispose()
        {
            StopHeartbeatTimer();
            _ws.Dispose();
        }


        public async Task OpenAsync()
        {
            using (var releaser = await _CommentSessionLock.LockAsync())
            {
                await _ws.OpenAsync();
            }

            if (!IsTimeshift)
            {
                await SendStartMessage();
            }
            else
            {
                await ResetConnectionForTimeshift(_startTime);
            }
        }

        public async void Close()
        {
            using (var releaser = await _CommentSessionLock.LockAsync())
            {
                StopHeartbeatTimer();
                _ws.Close(0x8, "");
            }
        }



        private async Task SendMessageAsync(string message)
        {
            using (var releaser = await _CommentSessionLock.LockAsync())
            {
                _ws.Send(message);
            }
        }


        /// <summary>
        /// Niwavidedタイプのコメントメッセージを整形して送信
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private Task SendNiwavidedMessage(string content)
        {
            var message = NiwavidedNicoLiveMessageHelper.MakeNiwavidedMessage(_MessageSendCount, content);
            return SendMessageAsync(message);
        }

        #region LiveStreaming

        /// <summary>
        /// Niwavided コメントサーバーへのスレッド受信を開始するためのメッセージを送信
        /// </summary>
        /// <returns></returns>
        private Task SendStartMessage()
        {
            return SendNiwavidedMessage(
                $"{{\"thread\":{{\"thread\":\"{ThreadId}\",\"version\":\"20061206\",\"fork\":0,\"user_id\":\"{UserId ?? "guest"}\",\"res_from\":-{FirstGetRecentMessageCount},\"with_global\":1,\"scores\":1,\"nicoru\":0}}}}"
                );
            
        }


        public void PostComment(string comment, string command, string postKey, TimeSpan elapsedTime)
        {
            if (IsTimeshift) { throw new InvalidOperationException(""); }
            if (UserId == null) { throw new InvalidOperationException("Post comment is require loggedIn."); }

            var vpos = (uint)elapsedTime.TotalMilliseconds / 10;
            var ticket = _ticket;

            _ = SendNiwavidedMessage(
                $"{{\"chat\":{{\"thread\":\"{ThreadId}\",\"vpos\":{vpos},\"mail\":\"{command}\",\"ticket\":\"{ticket}\",\"user_id\":\"{UserId}\",\"content\":\"{comment}\",\"postkey\":\"{postKey}\"}}}}"
                );
        }


        static readonly TimeSpan HEARTBEAT_INTERVAL = TimeSpan.FromMinutes(1);
        Timer HeartbeatTimer;

        private void StartHeartbeatTimer()
        {
            HeartbeatTimer = new Timer(_ =>
            {
                SendMessageAsync(string.Empty).ConfigureAwait(false);
            }
            , null, HEARTBEAT_INTERVAL, HEARTBEAT_INTERVAL);
        }

        private void StopHeartbeatTimer()
        {
            HeartbeatTimer?.Dispose();
            HeartbeatTimer = null;
        }


        #endregion



        #region Timeshift

        /// <summary>
        /// 
        /// </summary>
        /// <param name="res_from"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        private Task SendStartMessage_Timeshift(int res_from, DateTimeOffset when)
        {
            if (!IsTimeshift) { throw new InvalidOperationException(); }

            var whenString = when.ToUnixTimeSeconds().ToString();
            var waybackKey = _waybackkey;
            return SendNiwavidedMessage(
                $"{{\"thread\":{{\"thread\":\"{ThreadId}\",\"version\":\"20061206\",\"fork\":0,\"when\":{whenString},\"user_id\":\"{UserId ?? "guest"}\",\"res_from\":{res_from},\"with_global\":1,\"scores\":1,\"nicoru\":0,\"waybackkey\":\"{waybackKey}\"}}}}"
                );
        }

        public async void Seek(TimeSpan timeSpan)
        {
            if (!IsTimeshift) { throw new InvalidOperationException(); }

            //if (await _ws.OpenAsync())
            {
                _ = ResetConnectionForTimeshift(_startTime + timeSpan);
            }
        }

        private AsyncLock _CommentPullTimingTimerLock = new AsyncLock();
        private Timer _CommentPullTimingTimer;
        private DateTimeOffset _NextCommentPullTiming;

        private async Task ResetConnectionForTimeshift(DateTimeOffset initialTime)
        {
            if (!IsTimeshift) { throw new InvalidOperationException(); }

            using (var releaser = await _CommentPullTimingTimerLock.LockAsync())
            {
                StopCommentPullTimingTimer();

                // オープンからスタートまでのコメントをざっくり取得
                await SendStartMessage_Timeshift(-30, initialTime);

                _NextCommentPullTiming = initialTime + TimeSpan.FromSeconds(90);

                await SendStartMessage_Timeshift(_LastRes + 1, _NextCommentPullTiming);

                _NextCommentPullTiming = _NextCommentPullTiming + TimeSpan.FromSeconds(85);

                // 次のコメント取得の準備
                StartCommentPullTimingTimer();
            }
        }

        private void StopCommentPullTimingTimer()
        {
            _CommentPullTimingTimer?.Dispose();
        }

        private void StartCommentPullTimingTimer()
        {
            if (_CommentPullTimingTimer != null)
            {
                _CommentPullTimingTimer.Dispose();
                _CommentPullTimingTimer = null;
            }

            _CommentPullTimingTimer = new Timer(async _ =>
            {
                using (var releaser = await _CommentPullTimingTimerLock.LockAsync())
                {
                    //await _ws.OpenAsync();

                    await SendStartMessage_Timeshift(_LastRes + 1, _NextCommentPullTiming);

                    _NextCommentPullTiming = _NextCommentPullTiming + TimeSpan.FromSeconds(85);
                }
            }
            , null
            , TimeSpan.FromSeconds(85)
            , TimeSpan.FromSeconds(85)
            );
        }

        #endregion
    }
}
