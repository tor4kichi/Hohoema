using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NeoSmart.AsyncLock;
using WebSocket4Net;
using WebSocket = WebSocket4Net.WebSocket;
using NiconicoToolkit.Live.WatchSession;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Text.Encodings.Web;
using NiconicoToolkit.Live.WatchSession.ToClientMessage;

namespace NiconicoToolkit.Live.WatchSession
{
    public delegate void LiveStreamRecieveErrorHandler(ErrorMessageType errorMessageType);
    public delegate void LiveStreamRecieveServerTimeHandler(DateTime serverTime);
    public delegate void LiveStreamRecievePermitHandler(string parmit);
    public delegate void LiveStreamRecieveStreamHandler(Live2CurrentStreamEventArgs e);
    public delegate void LiveStreamRecieveRoomHandler(Live2CurrentRoomEventArgs e);
    public delegate void LiveStreamRecieveRoomsHandler(Live2RoomsEventArgs e);
    public delegate void LiveStreamRecieveStatisticsHandler(Live2StatisticsEventArgs e);
    public delegate void LiveStreamRecieveScheduleHandler(Live2ScheduleEventArgs e);
    public delegate void LiveStreamReceivedisconnectHandler(Live2DisconnectEventArgs e);
    public delegate void LiveStreamRecieveResconnectHandler(Live2ReconnectEventArgs e);
    public delegate void LiveStreamElapsedTimeUpdateEventHandler(LiveStreamElapsedTimeUpdateEventArgs e);

    public sealed class LiveStreamElapsedTimeUpdateEventArgs
    {
        public TimeSpan Time { get; set; }
        public int VideoPosition { get; set; }
        public bool Seeked { get; set; }
    }

    public class Live2CurrentStreamEventArgs
    {
        public string SyncUri { get; set; }
        public string Uri { get; set; }
        public LiveQualityType Quality { get; set; }
        public LiveQualityType[] AvailableQualities { get; set; }
        public string MediaServerType { get; set; }
        public string MediaServerAuth { get; set; }
        public string Protocol { get; set; }
    }

    public class Live2RoomsEventArgs
    {
        public Live2CurrentRoomEventArgs[] Rooms { get; set; }
    }


    public class Live2CurrentRoomEventArgs
    {
        public Uri MessageServerUrl { get; set; }
        public string MessageServerType { get; set; }
        public string RoomName { get; set; }
        public string ThreadId { get; set; }
        public bool IsFirst { get; set; }
        public string WaybackKey { get; set; }

        public LiveCommentSession CreateCommentClientForTimeshift(string userAgent, string userId, DateTimeOffset startTime)
        {
            return LiveCommentSession.CreateForTimeshift(MessageServerUrl.OriginalString, ThreadId, userId, userAgent, WaybackKey, startTime);
        }
        public LiveCommentSession CreateCommentClientForLiveStreaming(string userAgent, string userId)
        {
            return LiveCommentSession.CreateForLiveStream(MessageServerUrl.OriginalString, ThreadId, userId, userAgent);
        }
    }

    public class Live2StatisticsEventArgs
    {
        public long ViewCount { get; set; }
        public long CommentCount { get; set; }
        public long AdPoints { get; set; }
        public long GiftPoints { get; set; }
    }

    public class Live2ScheduleEventArgs
    {
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class Live2DisconnectEventArgs
    {
        public DisconnectReasonType ReasonType { get; set; }
    }

    public class Live2ReconnectEventArgs
    {
        public string AudienceToken { get; set; }
        public TimeSpan WaitTime { get; set; }
    }

    public sealed class Live2WatchSession : IDisposable
    {
        private WebSocket _ws { get; set; }

        AsyncLock _WebSocketLock = new AsyncLock();

        public readonly bool IsWatchWithTimeshift;
        private readonly string _webSocketUrl;
        private readonly JsonSerializerOptions _SocketJsonDeserializerOptions;
        private readonly JsonSerializerOptions _SocketJsonSerializerOptions;

        public event LiveStreamRecieveErrorHandler RecieveError;
        public event LiveStreamRecieveServerTimeHandler RecieveServerTime;
        public event LiveStreamRecieveStreamHandler RecieveStream;
        public event LiveStreamRecieveRoomHandler RecieveRoom;
        public event LiveStreamRecieveRoomsHandler RecieveRooms;
        public event LiveStreamRecieveStatisticsHandler RecieveStatistics;
        public event LiveStreamRecieveScheduleHandler RecieveSchedule;
        public event LiveStreamReceivedisconnectHandler ReceiveDisconnect;
        public event LiveStreamRecieveResconnectHandler RecieveReconnect;
        public event LiveStreamElapsedTimeUpdateEventHandler ElapsedTimeUpdated;

        internal Live2WatchSession(string webSocketUrl, bool isTimeshift)
        {
            IsWatchWithTimeshift = isTimeshift;
            _webSocketUrl = webSocketUrl;

            Dictionary<string, string> customHeaderItems = new Dictionary<string, string> 
            {
                {"host", "a.live2.nicovideo.jp"},
                {"upgrade", "websocket"},
                {"pragma", "no-cache"},
                {"cache-control", "no-cache"},
                {"connection", "upgrade"},
                
            };

            _ws = new WebSocket(_webSocketUrl, customHeaderItems: customHeaderItems.ToList(), origin: "https://live2.nicovideo.jp", userAgent: "NiconicoLiveToolkit_UWP");
            _ws.MessageReceived += _ws_MessageReceived;
            _ws.Opened += _ws_Opened;
            _ws.Closed += _ws_Closed;
            _ws.EnableAutoSendPing = false;
            

            _SocketJsonDeserializerOptions = new System.Text.Json.JsonSerializerOptions()
            {
                Converters =
                {
                    new JsonStringEnumMemberConverter(JsonSnakeCaseNamingPolicy.Instance),
                    new WatchSessionToClientMessageJsonConverter(),
                },
            };

            _SocketJsonSerializerOptions = new System.Text.Json.JsonSerializerOptions()
            {
                Converters =
                {
                    new JsonStringEnumMemberConverter(JsonSnakeCaseNamingPolicy.Instance),
                    new WatchSessionToServerMessageJsonConverter(),
                },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
        }

        Timer WatchingHeartbaetTimer;

        public async void Close()
        {
            using (var releaser = await _WebSocketLock.LockAsync())
            {
                _ws.Close(0x8, "");
                WatchingHeartbaetTimer?.Dispose();
            }
        }

        public void Dispose()
        {
            _ws.Dispose();
            WatchingHeartbaetTimer?.Dispose();
        }



        private void _ws_Opened(object sender, EventArgs e)
        {
        }

        private void _ws_Closed(object sender, EventArgs e)
        {
            WatchingHeartbaetTimer?.Dispose();
            WatchingHeartbaetTimer = null;
        }


        #region Message Received

        private void _ws_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Debug.WriteLine(e.Message);

            var data = JsonDeserializeHelper.Deserialize<WatchServerToClientMessage>(e.Message, _SocketJsonDeserializerOptions);
            _ = data switch
            {
                Error_WatchSessionToClientMessage error => ProcessErrorData(error),
                Seat_WatchSessionToClientMessage seat => ProcessSeetData(seat),
                Akashic_WatchSessionToClientMessage akashic => ProcessAkashicData(akashic),
                Postkey_WatchSessionToClientMessage postkey => ProcessPostKeyData(postkey),
                Stream_WatchSessionToClientMessage stream => ProcessStreamData(stream),
                Room_WatchSessionToClientMessage room => ProcessRoomData(room),
                Rooms_WatchSessionToClientMessage rooms => ProcessRoomsData(rooms),
                ServerTime_WatchSessionToClientMessage serverTime => ProcessServerTimeData(serverTime),
                Statistics_WatchSessionToClientMessage statistics => ProcessStatisticsData(statistics),
                Schedule_WatchSessionToClientMessage schedule => ProcessScheduleData(schedule),
                Ping_WatchSessionToClientMessage ping => ProcessPingData(ping),
                Disconnect_WatchSessionToClientMessage disconnect => ProcessDisconnectData(disconnect),
                Reconnect_WatchSessionToClientMessage reconnect => ProcessReconnectData(reconnect),
                PostCommentResult_WatchSessionToClientMessage postCommentResult => ProcessPostCommentResultData(postCommentResult),
                _ => true
            };
        }

        private bool ProcessErrorData(Error_WatchSessionToClientMessage error)
        {
            RecieveError?.Invoke(error.Code);
            return true;
        }

        private bool ProcessSeetData(Seat_WatchSessionToClientMessage seat)
        {
            WatchingHeartbaetTimer?.Dispose();
            WatchingHeartbaetTimer = new Timer((state) =>
            {
                _ = ((Live2WatchSession)state).SendMessageAsync(@"{""type"":""keepSeat""}");
            }
            , this, TimeSpan.Zero, TimeSpan.FromSeconds(seat.KeepIntervalSec)
            );

            return true;
        }

        private bool ProcessAkashicData(Akashic_WatchSessionToClientMessage akashic)
        {
            return true;
        }

        private bool ProcessPostKeyData(Postkey_WatchSessionToClientMessage postkey)
        {
            _GetPostkeyTaskCompletionSource.SetResult(postkey.Value);
            return true;
        }

        private bool ProcessStreamData(Stream_WatchSessionToClientMessage stream)
        {
            RecieveStream?.Invoke(new Live2CurrentStreamEventArgs()
            {
                Uri = stream.Uri,
                SyncUri = stream.SyncUri,
                Quality = stream.Quality,
                AvailableQualities = stream.AvailableQualities,
                Protocol = stream.Protocol
            });
            return true;
        }

        private bool ProcessRoomData(Room_WatchSessionToClientMessage room)
        {
            RecieveRoom?.Invoke(new Live2CurrentRoomEventArgs()
            {
                MessageServerUrl = room.MessageServer.Uri,
                MessageServerType = room.MessageServer.Type,
                RoomName = room.Name,
                ThreadId = room.ThreadId,
                IsFirst = room.IsFirst,
                WaybackKey = room.waybackkey
            });
            return true;
        }

        private bool ProcessRoomsData(Rooms_WatchSessionToClientMessage rooms)
        {
            RecieveRooms?.Invoke(new Live2RoomsEventArgs()
            {
                Rooms = rooms.Rooms.Select(x => new Live2CurrentRoomEventArgs() 
                {
                    MessageServerUrl = x.MessageServer.Uri,
                    MessageServerType = x.MessageServer.Type,
                    RoomName = x.Name,
                    ThreadId = x.ThreadId,
                }).ToArray()   
            }) ;
            return true;
        }

        private bool ProcessServerTimeData(ServerTime_WatchSessionToClientMessage serverTime)
        {
            RecieveServerTime?.Invoke(serverTime.CurrentTime);
            return true;
        }

        private bool ProcessStatisticsData(Statistics_WatchSessionToClientMessage statistics)
        {
            RecieveStatistics?.Invoke(new Live2StatisticsEventArgs()
            {
                ViewCount = statistics.Viewers ?? 0,
                CommentCount = statistics.comments ?? 0,
                AdPoints = statistics.adPoints ?? 0,
                GiftPoints = statistics.giftPoints ?? 0,
            });
            return true;
        }

        private bool ProcessScheduleData(Schedule_WatchSessionToClientMessage schedule)
        {
            RecieveSchedule?.Invoke(new Live2ScheduleEventArgs()
            {
                BeginTime = schedule.Begin,
                EndTime = schedule.End,
            });
            return true;
        }

        private bool ProcessPingData(Ping_WatchSessionToClientMessage ping)
        {
            _ = SendMessageAsync("{\"type\":\"pong\"}");
            return true;
        }

        private bool ProcessDisconnectData(Disconnect_WatchSessionToClientMessage disconnect)
        {
            ReceiveDisconnect?.Invoke(new Live2DisconnectEventArgs()
            {
                ReasonType = disconnect.Reason
            });
            return true;
        }

        private bool ProcessReconnectData(Reconnect_WatchSessionToClientMessage reconnect)
        {
            RecieveReconnect?.Invoke(new Live2ReconnectEventArgs() 
            {
                AudienceToken = reconnect.AudienceToken,
                WaitTime = TimeSpan.FromSeconds(reconnect.WaitTimeSec)
            });

            return true;
        }

        private bool ProcessPostCommentResultData(PostCommentResult_WatchSessionToClientMessage postCommentResult)
        {
            _PostCommentResultTaskCompletionSource.SetResult(postCommentResult.Chat);
            return true;
        }


        #endregion


        #region Send Message

        private async Task SendMessageAsync(string message)
        {
            using (var releaser = await _WebSocketLock.LockAsync())
            {
                _ws.Send(message);
                Debug.WriteLine("[Live2WatchSession <Send Message>]" + message);
            }
        }

        private async Task SendMessageAsync(WatchClientToServerMessageDataBase messageData)
        {
            var payload = new WatchClientToServerMessagePayload(messageData);
            var json = JsonSerializer.Serialize(payload, _SocketJsonSerializerOptions);
            await SendMessageAsync(json);
        }

        public async Task StartWachingAsync(LiveQualityType requestQuality, bool isLowLatency, LiveQualityLimitType? limit = null, bool? chasePlay = false)
        {
            bool isOpened = false;
            using (var releaser = await _WebSocketLock.LockAsync())
            {
                isOpened = await _ws.OpenAsync();
            }

            if (isOpened)
            {
                var startWachingMessage = new StartWatching_ToServerMessageData()
                {
                    Reconnect = false,
                    Room = new Room()
                    {
                        Protocol = "webSocket",
                        Commentable = !IsWatchWithTimeshift
                    },
                    Stream = new StartWatchingStream()
                    {
                        Quality = requestQuality,
                        Limit = limit,
                        Latency = isLowLatency ? LiveLatencyType.Low : LiveLatencyType.High,
                        ChasePlay = chasePlay
                    },
                };

                await SendMessageAsync(startWachingMessage);

                //if (string.IsNullOrEmpty(requestQuality))
                //{
                //    requestQuality = "high";
                //}
                //var startWatchingCommandText = $@"{{""type"":""startWatching"",""data"":{{""stream"":{{""quality"":""{requestQuality}"",""protocol"":""hls"",""latency"":""{(isLowLatency ? "low" : "high")}"",""chasePlay"":false}},""room"":{{""protocol"":""webSocket"",""commentable"":true}},""reconnect"":false}}}}";
                //await SendMessageAsync(startWatchingCommandText);
            }
        }

        public Task<bool> CloseAsync()
        {
            return _ws.CloseAsync();
        }

        public async Task ChangeStreamAsync(LiveQualityType requestQuality, bool isLowLatency, LiveQualityLimitType? limit = null, bool? chasePlay = false)
        {
            var changeStreamMessage = new ChangeStream_ToServerMessageData()
            {
                Quality = requestQuality,
                Limit = limit,
                Latency = isLowLatency ? LiveLatencyType.Low : LiveLatencyType.High,
                ChasePlay = chasePlay
            };

            await SendMessageAsync(changeStreamMessage);

            //var message = $"{{\"type\":\"changeStream\",\"data\":{{\"quality\":\"{quality}\",\"protocol\":\"hls\",\"latency\":\"{(isLowLatency ? "low" : "high")}\",\"chasePlay\":false}}}}";
            //return SendMessageAsync(message);
        }

        TaskCompletionSource<PostCommentResultChat> _PostCommentResultTaskCompletionSource;
        public async Task<PostCommentResultChat> PostCommentAsync(string text, TimeSpan videoPosition, bool isAnonymous, string size = null, string position = null, string color = null, string font = null)
        {
            var postCommentMessage = new PostComment_ToServerMessageData()
            { 
                Text = text,
                Vpos = (int) (videoPosition.TotalSeconds * 100),
                IsAnonymous = isAnonymous,
                Size = size,
                Position = position,
                Color = color,
                Font = font
            };

            _PostCommentResultTaskCompletionSource = new TaskCompletionSource<PostCommentResultChat>();
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                {
                    await SendMessageAsync(postCommentMessage);
                }
            }
            catch (TaskCanceledException)
            {
                _PostCommentResultTaskCompletionSource.SetCanceled();
            }

            return await _PostCommentResultTaskCompletionSource.Task;
        }


        TaskCompletionSource<string> _GetPostkeyTaskCompletionSource;


        [Obsolete("※ 部屋統合後は取得できません")]
        public async Task<string> GetPostkeyAsync()
        {
            _GetPostkeyTaskCompletionSource = new TaskCompletionSource<string>();
            await SendMessageAsync(
                $"{{\"type\":\"getPostkey\"}}"
                );

            try
            {
                using (var cancelToken = new CancellationTokenSource(1000))
                {
                }
            }
            catch (TaskCanceledException)
            {
                _GetPostkeyTaskCompletionSource.SetCanceled();
            }

            return await _GetPostkeyTaskCompletionSource.Task;
        }

        #endregion
    }
}
