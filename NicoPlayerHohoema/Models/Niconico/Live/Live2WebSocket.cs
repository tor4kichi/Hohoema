using Mntone.Nico2.Live.Watch;
using Newtonsoft.Json.Linq;
using NicoPlayerHohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace NicoPlayerHohoema.Models.Live
{
    public delegate void LiveStreamRecieveServerTimeHandler(DateTime serverTime);
    public delegate void LiveStreamRecievePermitHandler(string parmit);
    public delegate void LiveStreamRecieveCurrentStreamHandler(Live2CurrentStreamEventArgs e);
    public delegate void LiveStreamRecieveCurrentRoomHandler(Live2CurrentRoomEventArgs e);
    public delegate void LiveStreamRecieveStatisticsHandler(Live2StatisticsEventArgs e);
    public delegate void LiveStreamRecieveWatchIntervalHandler(TimeSpan intervalTime);
    public delegate void LiveStreamRecieveScheduleHandler(Live2ScheduleEventArgs e);
    public delegate void LiveStreamRecieveDisconnectHandler();


    public class Live2CurrentStreamEventArgs
    {
        public string Uri { get; set; }
        public string Quality { get; set; }
        public string[] AvailableQualities { get; set; }
        public string MediaServerType { get; set; }
        public string MediaServerAuth { get; set; }
        public string Protocol { get; set; }
    }

    public class Live2CurrentRoomEventArgs
    {
        public string MessageServerUrl { get; set; }
        public string MessageServerType { get; set; }
        public string RoomName { get; set; }
        public string ThreadId { get; set; }
        public bool IsFirst { get; set; }
        public string WaybackKey { get; set; }
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

    public sealed class Live2WebSocket : IDisposable
    {
        MessageWebSocket MessageWebSocket { get; }

        Mntone.Nico2.Live.Watch.Crescendo.CrescendoLeoProps Props { get; }

        AsyncLock _WebSocketLock = new AsyncLock();
        DataWriter _DataWriter;

        private bool _IsWatchWithTimeshift => Props?.Program.Status == "ENDED";

        public event LiveStreamRecieveServerTimeHandler RecieveServerTime;
        public event LiveStreamRecieveCurrentStreamHandler RecieveCurrentStream;
        public event LiveStreamRecieveCurrentRoomHandler RecieveCurrentRoom;
        public event LiveStreamRecieveStatisticsHandler RecieveStatistics;
        public event LiveStreamRecieveScheduleHandler RecieveSchedule;
        public event LiveStreamRecieveDisconnectHandler RecieveDisconnect;

        public Live2WebSocket(Mntone.Nico2.Live.Watch.Crescendo.CrescendoLeoProps props)
        {
            Props = props;
            MessageWebSocket = new MessageWebSocket();
            MessageWebSocket.Control.MessageType = SocketMessageType.Utf8;
            MessageWebSocket.MessageReceived += MessageWebSocket_MessageReceived;
            MessageWebSocket.ServerCustomValidationRequested += MessageWebSocket_ServerCustomValidationRequested;
            MessageWebSocket.Closed += MessageWebSocket_Closed;
        }

        Timer WatchingHeartbaetTimer;


        public async Task StartAsync(string requestQuality = "", bool isLowLatency = true)
        {
            var broadcastId = Props.Program.BroadcastId;
            var audienceToken = Props.Player.AudienceToken;
            using (var releaser = await _WebSocketLock.LockAsync())
            {
                Debug.WriteLine($"providerType: {Props.Program.ProviderType}");
                Debug.WriteLine($"comment websocket url: {Props.Site.Relive.WebSocketUrl}");

                await MessageWebSocket.ConnectAsync(new Uri(Props.Site.Relive.WebSocketUrl));
                _DataWriter = new DataWriter(MessageWebSocket.OutputStream);
            }

            if (string.IsNullOrEmpty(requestQuality))
            {
                requestQuality = "high";
            }

            var startWatchingCommandText = $@"{{""type"":""startWatching"",""data"":{{""stream"":{{""quality"":""{requestQuality}"",""protocol"":""hls"",""latency"":""{(isLowLatency ? "low" : "high")}"",""chasePlay"":false}},""room"":{{""protocol"":""webSocket"",""commentable"":true}},""reconnect"":false}}}}";
            await SendMessageAsync(startWatchingCommandText);
        }

        private async Task SendMessageAsync(string message)
        {
            using (var releaser = await _WebSocketLock.LockAsync())
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


        public Task SendChangeQualityMessageAsync(string quality, bool isLowLatency = true)
        {
            var message = $"{{\"type\":\"changeStream\",\"data\":{{\"quality\":\"{quality}\",\"protocol\":\"hls\",\"latency\":\"{(isLowLatency ? "low" : "high")}\",\"chasePlay\":false}}}}";
            return SendMessageAsync(message);
        }


        public async void Close()
        {
            using (var releaser = await _WebSocketLock.LockAsync())
            {
                MessageWebSocket.Close(0x8, "");
                WatchingHeartbaetTimer?.Dispose();
            }
        }

        public void Dispose()
        {
            MessageWebSocket.Dispose();
            WatchingHeartbaetTimer?.Dispose();
        }

        private async void MessageWebSocket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            string recievedText = string.Empty;
            using (var releaser = await _WebSocketLock.LockAsync())
            {
                using (var reader = new StreamReader(args.GetDataStream().AsStreamForRead()))
                {
                    recievedText = reader.ReadToEnd();
                    Debug.WriteLine($"<WebSocket Message> {args.MessageType}: {recievedText}");
                }
            }

            var param = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(recievedText);

            var type = (string)param["type"];
            var data = (JObject)param["data"];
            switch (type)
            {
                case "serverTime":
                    var serverTime = DateTime.Parse((string)data["currentMs"]);
                    RecieveServerTime?.Invoke(serverTime);
                    break;
                case "stream":
                    var currentStreamArgs = new Live2CurrentStreamEventArgs()
                    {
                        Uri = (string)data["uri"],
                        Quality = (string)data["quality"],
                        AvailableQualities = ((JArray)data["availableQualities"]).Select(x => x.ToString()).ToArray(),
                        Protocol = (string)data["protocol"]
                    };
                    RecieveCurrentStream?.Invoke(currentStreamArgs);
                    break;
                case "room":
                    var messageServer = (JObject)data["messageServer"];
                    var currentRoomArgs = new Live2CurrentRoomEventArgs()
                    {
                        MessageServerUrl = (string)messageServer["uri"],
                        MessageServerType = (string)messageServer["type"],
                        RoomName = (string)data["name"],
                        ThreadId = (string)data["threadId"],
                        IsFirst = (bool)data["isFirst"],
                        WaybackKey = (string)data["waybackkey"]
                    };
                    RecieveCurrentRoom?.Invoke(currentRoomArgs);
                    break;
                case "seat":
                    var intervalSec = (int)data["keepIntervalSec"];
                    WatchingHeartbaetTimer?.Dispose();
                    WatchingHeartbaetTimer = new Timer((state) =>
                    {
                        _= SendMessageAsync(@"{""type"":""keepSeat""}");
                    }
                    , null, TimeSpan.FromSeconds(intervalSec), TimeSpan.FromSeconds(intervalSec)
                    );
                    break;
                case "statistics":
                    var statisticsArgs = new Live2StatisticsEventArgs()
                    {
                        ViewCount = data.TryGetValue("viewers", out var viewers) ? (int)viewers : 0,
                        CommentCount = data.TryGetValue("comments", out var comments) ? (int)comments : 0,
                        AdPoints = data.TryGetValue("adPoints", out var adPoint) ? (int)adPoint : 0,
                        GiftPoints = data.TryGetValue("giftPoints", out var giftPoints) ? (int)giftPoints : 0,
                    };
                    RecieveStatistics?.Invoke(statisticsArgs);
                    break;
                case "schedule":
                    var scheduleArgs = new Live2ScheduleEventArgs()
                    {
                        BeginTime = DateTime.Parse((string)data["begin"]),
                        EndTime = DateTime.Parse((string)data["end"]),
                    };
                    RecieveSchedule?.Invoke(scheduleArgs);
                    break;
                case "disconnect":
                    // "END_PROGRAM" "TAKEOVER"(追い出し) など
                    var endReason = (string)data["reason"];
                    RecieveDisconnect?.Invoke();
                    break;
                case "postkey":
                    var postKey = (string)data["value"];
                    //var expireAt = DateTime.Parse((string)data["expireAt"]);

                    _Postkey = postKey;
                    break;
                case "ping":
                    await SendMessageAsync("{\"type\":\"pong\"}");
                    break;
            }
        }

        private async void MessageWebSocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            using (var releaser = await _WebSocketLock.LockAsync())
            {
                Debug.WriteLine($"<WebScoket Closed> {args.Code}: {args.Reason}");
            }

            WatchingHeartbaetTimer?.Dispose();
            WatchingHeartbaetTimer = null;
        }

        private async void MessageWebSocket_ServerCustomValidationRequested(MessageWebSocket sender, WebSocketServerCustomValidationRequestedEventArgs args)
        {
            using (var releaser = await _WebSocketLock.LockAsync())
            {
                Debug.WriteLine(args.ToString());
            }
        }


        string _Postkey;
        public async Task<string> GetPostkeyAsync(string threadId)
        {
            _Postkey = null;
            await SendMessageAsync(
                $"{{\"type\":\"getPostkey\"}}"
                );

            using (var cancelToken = new CancellationTokenSource(1000))
            {
                while (_Postkey == null)
                {
                    await Task.Delay(1);
                }
            }

            return _Postkey;
        }
       
    }
}
