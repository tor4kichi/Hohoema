using Newtonsoft.Json.Linq;
using Hohoema.Models.Helpers;
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

namespace Hohoema.Models.Live.Niwavided
{
    /* HTML5版タイムシフトにおけるコメント取得の概要
     * 
     * WebSocketを経由したコメント取得で、送受信するデータのフォーマット（Niwavided形式？）自体は生放送と同じ。
     * ただ、生放送では次々とコメントが送られてくる挙動のため接続したらあとは受信するだけでよかったが
     * タイムシフトでは、「この時間(when)までのコメントをこのコメント番号（res_from）から送ってください」という
     * やり取りを90秒ごとに仕掛ける必要があります。
     */

    public sealed class NiwavidedNicoTimeshiftCommentClient : INicoLiveCommentClient, IDisposable
    {
        public bool IsConnected { get; private set; }

        public event EventHandler<CommentPostedEventArgs> CommentPosted;
        public event EventHandler<CommentRecievedEventArgs> CommentRecieved;
        public event EventHandler<CommentServerConnectedEventArgs> Connected;
        public event EventHandler<CommentServerDisconnectedEventArgs> Disconnected;


        MessageWebSocket _CommentSessionWebSocket;

        DataWriter _DataWriter;
        Models.Helpers.AsyncLock _CommentSessionLock = new Models.Helpers.AsyncLock();

        public CommentSessionInfo CommentSessionInfo { get; private set; }
        Mntone.Nico2.Videos.Comment.NGMS_Thread_Response _Thread;

        public string WaybackKey { get; }
        public DateTimeOffset StartTime { get; }
        private DateTimeOffset _NextTime;
        int _MessageSendCount = 0;


        int _LastRes = 0;

        public NiwavidedNicoTimeshiftCommentClient(string messageServerUrl, string threadId, string userId, string waybackkey, DateTimeOffset startTime)
        {
            StartTime = startTime;

            WaybackKey = waybackkey;
            CommentSessionInfo = new CommentSessionInfo()
            {
                MessageServerUrl = messageServerUrl,
                ThreadId = threadId,
                UserId = userId
            };
        }

        

        public void Dispose()
        {
            StopReOpenTimer();

            Close();
        }

        private async Task CreateAndConnectWebSocket(Uri uri)
        {
            using (var releaser = await _CommentSessionLock.LockAsync())
            {
                if (IsConnected) { return; }

                if (_CommentSessionWebSocket != null)
                {
                    Close();
                }

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

                await _CommentSessionWebSocket.ConnectAsync(uri);
                _DataWriter = new DataWriter(_CommentSessionWebSocket.OutputStream);
            }
        }

        AsyncLock _ReOpenTimerLock = new AsyncLock();

        private async Task ResetConnection(DateTimeOffset initialTime)
        {
            using (var releaser = await _ReOpenTimerLock.LockAsync())
            {
                StopReOpenTimer();

                await CreateAndConnectWebSocket(new Uri(CommentSessionInfo.MessageServerUrl));

                // オープンからスタートまでのコメントをざっくり取得
                await SendStartMessage_Timeshift(-30, initialTime);
                _NextTime = initialTime + TimeSpan.FromSeconds(90);

                await SendStartMessage_Timeshift(_LastRes + 1, _NextTime);

                _NextTime = _NextTime + TimeSpan.FromSeconds(85);

                // 次のコメント取得の準備
                StartReOpenTimer();

                await Task.Delay(TimeSpan.FromSeconds(3));

                Close();
            }
        }

        public async void Open()
        {
            await ResetConnection(StartTime);
        }


        public async void Seek(TimeSpan timeSpan)
        {
            await ResetConnection(StartTime + timeSpan);
        }


        Timer _ReOpenTimer;

        private void StartReOpenTimer()
        {
            if (_ReOpenTimer != null)
            {
                _ReOpenTimer.Dispose();
                _ReOpenTimer = null;
            }

            _ReOpenTimer = new Timer(async _ => 
            {
                using (var releaser = await _ReOpenTimerLock.LockAsync())
                {
                    await CreateAndConnectWebSocket(new Uri(CommentSessionInfo.MessageServerUrl));

                    await SendStartMessage_Timeshift(_LastRes + 1, _NextTime);

                    _NextTime = _NextTime + TimeSpan.FromSeconds(85);
                }

                await Task.Delay(TimeSpan.FromSeconds(3));

                using (var releaser = await _ReOpenTimerLock.LockAsync())
                {
                    Close();
                }
            }
            , null
            , TimeSpan.FromSeconds(85)
            , TimeSpan.FromSeconds(85)
            );
        }

        private void StopReOpenTimer()
        {
            _ReOpenTimer?.Dispose();
        }

        public void Close()
        {
            _DataWriter?.DetachStream();
            _DataWriter = null;

            _CommentSessionWebSocket?.Close(1000, "");
            _CommentSessionWebSocket = null;
            IsConnected = false;
        }


        private void _CommentSessionWebSocket_ServerCustomValidationRequested(MessageWebSocket sender, WebSocketServerCustomValidationRequestedEventArgs args)
        {
            Debug.WriteLine("niwavided comment timeshift, ServerCustomValidationRequested");
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

                    IsConnected = true;

                    _LastRes = _Thread.LastRes == 0 ? _LastRes : _Thread.LastRes;

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

                await Task.Delay(1);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="res_from"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        private Task SendStartMessage_Timeshift(int res_from, DateTimeOffset when)
        {
            var info = this.CommentSessionInfo;
            var whenString = when.ToUnixTimeSeconds().ToString();
            var waybackKey = WaybackKey;
            return SendNiwavidedMessage(
                $"{{\"thread\":{{\"thread\":\"{info.ThreadId}\",\"version\":\"20061206\",\"fork\":0,\"when\":{whenString},\"user_id\":\"{info.UserId}\",\"res_from\":{res_from},\"with_global\":1,\"scores\":1,\"nicoru\":0,\"waybackkey\":\"{waybackKey}\"}}}}"
                );

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

        
        public void PostComment(string comment, string command, string postKey, TimeSpan elapsedTime)
        {
            throw new NotSupportedException("it's timeshift, can not posting comment.");
        }
    }
}
