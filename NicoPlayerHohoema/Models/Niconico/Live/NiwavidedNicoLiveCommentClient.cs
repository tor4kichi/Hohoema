using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace NicoPlayerHohoema.Models.Live.Niwavided
{
    public sealed class CommentSessionInfo
    {
        public string MessageServerUrl { get; set; }
        public string UserId { get; set; }
        public string ThreadId { get; set; }
    }

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


    public sealed class NiwavidedNicoLiveCommentClient : INicoLiveCommentClient, IDisposable
    {
        const uint FirstGetRecentMessageCount = 1000;

        MessageWebSocket _CommentSessionWebSocket { get; }
        DataWriter _DataWriter;
        Helpers.AsyncLock _CommentSessionLock = new Helpers.AsyncLock();

        public CommentSessionInfo CommentSessionInfo { get; private set; }
        Mntone.Nico2.Videos.Comment.NGMS_Thread_Response _Thread;

        public event EventHandler<CommentPostedEventArgs> CommentPosted;
        public event EventHandler<CommentRecievedEventArgs> CommentRecieved;
        public event EventHandler<CommentServerConnectedEventArgs> Connected;
        public event EventHandler<CommentServerDisconnectedEventArgs> Disconnected;

        public bool IsConnected { get; private set; }

        HttpClient _HttpClient;



        public NiwavidedNicoLiveCommentClient(string messageServerUrl, string threadId, string userId, HttpClient httpClient)
        {
            CommentSessionInfo = new CommentSessionInfo()
            {
                MessageServerUrl = messageServerUrl,
                ThreadId = threadId,
                UserId = userId
            };
            _HttpClient = httpClient;
            _CommentSessionWebSocket = new MessageWebSocket();
            _CommentSessionWebSocket.Control.MessageType = SocketMessageType.Utf8;
            _CommentSessionWebSocket.Control.SupportedProtocols.Add("msg.nicovideo.jp#json");

            _CommentSessionWebSocket.SetRequestHeader("Pragma", "not-cache");
            _CommentSessionWebSocket.SetRequestHeader("Sec-WebSocket-Extensions", "permessage-deflate");
            _CommentSessionWebSocket.SetRequestHeader("Sec-WebSocket-Extensions", "client_max_window_bits");
            _CommentSessionWebSocket.SetRequestHeader("User-Agent", "Hohoema_UWP");

            _CommentSessionWebSocket.MessageReceived += _CommentSessionWebSocket_MessageReceived;
            _CommentSessionWebSocket.ServerCustomValidationRequested += _CommentSessionWebSocket_ServerCustomValidationRequested;
            _CommentSessionWebSocket.Closed += _CommentSessionWebSocket_Closed;
        }

        private void _CommentSessionWebSocket_ServerCustomValidationRequested(MessageWebSocket sender, WebSocketServerCustomValidationRequestedEventArgs args)
        {
            Debug.WriteLine("niwavided comment ServerCustomValidationRequested");
        }

        private async void _CommentSessionWebSocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            using (var releaser = await _CommentSessionLock.LockAsync())
            {
                Debug.WriteLine($"<CommentSession Closed> {args.Code}: {args.Reason}");
            }
        }

        private async void _CommentSessionWebSocket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            string recievedText = string.Empty;
            using (var releaser = await _CommentSessionLock.LockAsync())
            {
                using (var reader = new StreamReader(args.GetDataStream().AsStreamForRead()))
                {
                    recievedText = reader.ReadToEnd();
                    Debug.WriteLine($"<CommentSession Message> {args.MessageType}: {recievedText}");
                }

                var jsonObject = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(recievedText);

                if (jsonObject.TryGetValue("chat", out var chatJson))
                {
                    var chat = chatJson.ToObject<Mntone.Nico2.Videos.Comment.NMSG_Chat>();

                    var liveChatData = new LiveChatData()
                    {
                        Thread = chat.Thread,
                        No = chat.No,
                        Content = chat.Content,
                        Date = chat.Date,
                        DateUsec = chat.DateUsec,
                        IsAnonymity = chat.Anonymity == 1,
                        __Premium = chat.Premium,
                        IsYourPost = chat.Yourpost == 1,
                        Mail = chat.Mail,
                        Score = chat.Score,
                        UserId = chat.UserId,
                        Vpos = chat.Vpos
                    };

                    CommentRecieved?.Invoke(this, new CommentRecievedEventArgs()
                    {
                        Chat = liveChatData
                    });
                }
                else if (jsonObject.TryGetValue("chat_result", out var chatResultJson))
                {
                    var chatResult = chatResultJson.ToObject<Mntone.Nico2.Videos.Comment.Chat_result>();
                    CommentPosted?.Invoke(this, new CommentPostedEventArgs()
                    {
                        Thread = chatResult.Thread,
                        ChatResult = chatResult.Status,
                        No = chatResult.No
                    });

                }
                else if (jsonObject.TryGetValue("thread", out var threadJson))
                {
                    var thread = threadJson.ToObject<Mntone.Nico2.Videos.Comment.NGMS_Thread_Response>();
                    _Thread = thread;

                    //
                    // コメントの受信開始を受け取ってからハートビートを開始する
                    // 
                    StartHeartbeatTimer();

                    Connected?.Invoke(this, new CommentServerConnectedEventArgs()
                    {
                        LastRes = thread.LastRes,
                        Resultcode = thread.Resultcode,
                        Revision = thread.Revision,
                        ServerTime = thread.ServerTime,
                        Thread = thread.Thread,
                        Ticket = thread.Ticket,
                    });
                }
                else if (jsonObject.TryGetValue("ping", out var pingJson))
                {
                    var ping = pingJson.ToObject<Mntone.Nico2.Videos.Comment.Ping>();

                    // {"ping":{"content":"rs:1"}}
                    // { "ping":{ "content":"ps:5"} }
                    // {"chat_result":{...}}
                    // {"chat":{...}} 
                    // { "ping":{ "content":"pf:5"} }
                    // { "ping":{ "content":"rf:1"} }

                    if (ping.Content.StartsWith("pf"))
                    {
                        _MessageSendCount += 1;
                    }
                }
                else
                {
                    Debug.WriteLine("Not Support");
                }
            }
        }

        public void Dispose()
        {
            StopHeartbeatTimer();
            _CommentSessionWebSocket.Dispose();
        }


        public async void Open()
        {
            using (var releaser = await _CommentSessionLock.LockAsync())
            {
                if (IsConnected) { return; }

                var uri = new Uri(CommentSessionInfo.MessageServerUrl);
                await _CommentSessionWebSocket.ConnectAsync(uri);
                _DataWriter = new DataWriter(_CommentSessionWebSocket.OutputStream);
            }

            await SendStartMessage();
        }

        public async void Close()
        {
            using (var releaser = await _CommentSessionLock.LockAsync())
            {
                StopHeartbeatTimer();
                _CommentSessionWebSocket.Close(0x8, "");
            }
        }



        private async Task SendMessageAsync(string message)
        {
            using (var releaser = await _CommentSessionLock.LockAsync())
            {
                if (_DataWriter != null)
                {
                    try
                    {
                        _DataWriter.WriteString(message);
                        await _DataWriter.StoreAsync();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                }
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

        private int _MessageSendCount = 0;

        /// <summary>
        /// Niwavided コメントサーバーへのスレッド受信を開始するためのメッセージを送信
        /// </summary>
        /// <returns></returns>
        private Task SendStartMessage()
        {
            var info = this.CommentSessionInfo;
            return SendNiwavidedMessage(
                $"{{\"thread\":{{\"thread\":\"{info.ThreadId}\",\"version\":\"20061206\",\"fork\":0,\"user_id\":\"{info.UserId}\",\"res_from\":-{FirstGetRecentMessageCount},\"with_global\":1,\"scores\":1,\"nicoru\":0}}}}"
                );
            
        }

        

        /// <summary>
        /// コメントを送信する
        /// </summary>
        /// <param name="content"></param>
        /// <param name="command"></param>
        /// <param name="postKey">see@ Live2WebSocket.GetPostKeyAsync()</param>
        /// <param name="time"></param>
        /// <returns></returns>
        public void PostComment(string content, string command, string postKey, TimeSpan time)
        {
            var info = this.CommentSessionInfo;
            var vpos = (uint)time.TotalMilliseconds / 10;
            var ticket = _Thread.Ticket;

            SendNiwavidedMessage(
                $"{{\"chat\":{{\"thread\":\"{info.ThreadId}\",\"vpos\":{vpos},\"mail\":\"{command}\",\"ticket\":\"{ticket}\",\"user_id\":\"{info.UserId}\",\"content\":\"{content}\",\"postkey\":\"{postKey}\"}}}}"
                )
                .ConfigureAwait(false);
        }

       

        #region Heartbeat

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
    }
}
