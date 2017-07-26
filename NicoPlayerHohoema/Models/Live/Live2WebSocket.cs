using Mntone.Nico2.Live.Watch;
using Newtonsoft.Json.Linq;
using NicoPlayerHohoema.Util;
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


    public class Live2CurrentStreamEventArgs
    {
        public string Uri { get; set; }
        public string Name { get; set; }
        public string Quality { get; set; }
        public string[] QualityTypes { get; set; }
        public string MediaServerType { get; set; }
        public string MediaServerAuth { get; set; }
        public string StreamingProtocol { get; set; }
    }

    public class Live2CurrentRoomEventArgs
    {
        public string MessageServerUrl { get; set; }
        public string MessageServerType { get; set; }
        public string RoomName { get; set; }
        public string ThreadId { get; set; }
        public int[] Forks { get; set; }
        public int[] ImportedForks { get; set; }
    }

    public class Live2StatisticsEventArgs
    {
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int Count_3 { get; set; }
        public int Count_4 { get; set; }
    }

    public class Live2ScheduleEventArgs
    {
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public sealed class Live2WebSocket : IDisposable
    {
        MessageWebSocket MessageWebSocket { get; }

        LeoPlayerProps Props { get; }

        AsyncLock _WebSocketLock = new AsyncLock();
        DataWriter _DataWriter;



        public event LiveStreamRecieveServerTimeHandler RecieveServerTime;
        public event LiveStreamRecievePermitHandler RecievePermit;
        public event LiveStreamRecieveCurrentStreamHandler RecieveCurrentStream;
        public event LiveStreamRecieveCurrentRoomHandler RecieveCurrentRoom;
        public event LiveStreamRecieveStatisticsHandler RecieveStatistics;
        public event LiveStreamRecieveWatchIntervalHandler RecieveWatchInterval;
        public event LiveStreamRecieveScheduleHandler RecieveSchedule;


        public Live2WebSocket(LeoPlayerProps props)
        {
            Props = props;
            MessageWebSocket = new MessageWebSocket();
            MessageWebSocket.Control.MessageType = SocketMessageType.Utf8;
            MessageWebSocket.MessageReceived += MessageWebSocket_MessageReceived;
            MessageWebSocket.ServerCustomValidationRequested += MessageWebSocket_ServerCustomValidationRequested;
            MessageWebSocket.Closed += MessageWebSocket_Closed;
        }



        public async Task StartAsync()
        {
            using (var releaser = await _WebSocketLock.LockAsync())
            {
                var url = $"{Props.WebSocketBaseUrl}{Props.BroadcastId}?audience_token={Props.AudienceToken}";
                await MessageWebSocket.ConnectAsync(new Uri(url));
                _DataWriter = new DataWriter(MessageWebSocket.OutputStream);
            }

            await SendMessageAsync($"{{\"type\":\"watch\",\"body\":{{\"params\":[\"{Props.BroadcastId}\",\"\",\"true\",\"hls\",\"\"],\"command\":\"getpermit\"}}}}");
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



        public async void Close()
        {
            using (var releaser = await _WebSocketLock.LockAsync())
            {
                MessageWebSocket.Close(0x8, "");
            }
        }

        public void Dispose()
        {
            MessageWebSocket.Dispose();
        }

        private async void MessageWebSocket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            string recievedText = string.Empty;
            using (var releaser = await _WebSocketLock.LockAsync())
            {
                using (var reader = new StreamReader(args.GetDataStream().AsStreamForRead()))
                {
                    recievedText = reader.ReadToEnd();
                    Debug.WriteLine($"{args.MessageType}: {recievedText}");

                    
                }
            }

            var param = (dynamic)Newtonsoft.Json.JsonConvert.DeserializeObject(recievedText);

            if (param.type == "watch")
            {
                var body = param.body;
                var command = (string)body.command;
                switch (command)
                {
                    case "servertime":
                        
                        var paramItems = body["params"];
                        var serverTimeString = paramItems[0].ToString();
                        var serverTimeTick = long.Parse(serverTimeString);
                        var serverTime = DateTime.FromBinary(serverTimeTick);
                        RecieveServerTime?.Invoke(serverTime);
                        break;
                    case "permit":
                        var permit = body["params"][0];
                        RecievePermit?.Invoke(permit);
                        break;
                    case "currentstream":

                        var current_stream = body.currentStream;
                        var currentStreamArgs = new Live2CurrentStreamEventArgs()
                        {
                            Uri = current_stream.uri,
                            Name = current_stream.name,
                            Quality = current_stream.quality,
                            QualityTypes = ((JToken)current_stream.qualityTypes).Select(x => x.ToString()).ToArray(),
                            MediaServerType = current_stream.mediaServerType,
                            MediaServerAuth = current_stream.mediaServerAuth,
                            StreamingProtocol = current_stream.streamingProtocol
                        };
                        RecieveCurrentStream?.Invoke(currentStreamArgs);
                        break;
                    case "currentroom":
                        var room = body.room;
                        var currentRoomArgs = new Live2CurrentRoomEventArgs()
                        {
                            MessageServerUrl = room.messageServerUri,
                            MessageServerType = room.messageServerType,
                            RoomName = room.roomName,
                            ThreadId = room.threadId,
                            Forks = ((JToken)room.forks).Select(x => (int)x).ToArray(),
                            ImportedForks = ((JToken)room.importedForks).Select(x => (int)x).ToArray()
                        };
                        RecieveCurrentRoom?.Invoke(currentRoomArgs);
                        break;
                    case "statistics":
                        var countItems = ((JToken)body["params"]).Select(x => x.ToString()).ToArray();
                        var statisticsArgs = new Live2StatisticsEventArgs()
                        {
                            ViewCount = int.Parse(countItems[0]),
                            CommentCount = int.Parse(countItems[1]),
                            Count_3 = int.Parse(countItems[2]),
                            Count_4 = int.Parse(countItems[3]),
                        };
                        RecieveStatistics?.Invoke(statisticsArgs);
                        break;
                    case "watchinginterval":
                        var timeString = ((JToken)body["params"]).Select(x => x.ToString()).ToArray()[0];
                        var time = TimeSpan.FromSeconds(int.Parse(timeString));
                        RecieveWatchInterval?.Invoke(time);
                        break;
                    case "schedule":
                        var scheduleArgs = new Live2ScheduleEventArgs()
                        {
                            BeginTime = DateTime.FromBinary((long)body.update.begintime),
                            EndTime = DateTime.FromBinary((long)body.update.endtime),
                        };
                        RecieveSchedule?.Invoke(scheduleArgs);
                        break;
                }
            }
            else if (param.type == "ping")
            {
                await SendMessageAsync(recievedText);
            }
        }

        private async void MessageWebSocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            using (var releaser = await _WebSocketLock.LockAsync())
            {
                Debug.WriteLine($"{args.Code}: {args.Reason}");
            }
        }

        private async void MessageWebSocket_ServerCustomValidationRequested(MessageWebSocket sender, WebSocketServerCustomValidationRequestedEventArgs args)
        {
            using (var releaser = await _WebSocketLock.LockAsync())
            {
                Debug.WriteLine(args.ToString());
            }
        }

       
    }
}
