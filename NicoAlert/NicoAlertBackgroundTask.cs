using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel.Background;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Hohoema.NicoAlert
{
    // https://docs.microsoft.com/ja-jp/windows/uwp/networking/network-communications-in-the-background#socket-broker-and-the-socketactivitytrigger

    public abstract class NicoAlertBackgroundTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var defferal = taskInstance.GetDeferral();
            try
            {
                var socketActivityTriggerDetails = taskInstance.TriggerDetails as SocketActivityTriggerDetails;
                if (socketActivityTriggerDetails == null) { return; }

                var socketInfomation = socketActivityTriggerDetails.SocketInformation;
                var socket = socketInfomation?.StreamSocket;
                switch (socketActivityTriggerDetails.Reason)
                {
                    case SocketActivityTriggerReason.None:
                        break;
                    case SocketActivityTriggerReason.SocketActivity:
                        try
                        {
                            if (socket != null)
                            {
                                await ReadStream(socket);
                            }
                        }
                        finally
                        {
                            socket.TransferOwnership(socketInfomation.Id);
                        }
                        break;
                    case SocketActivityTriggerReason.ConnectionAccepted:
                        break;
                    case SocketActivityTriggerReason.KeepAliveTimerExpired:
                        break;
                    case SocketActivityTriggerReason.SocketClosed:
                        break;
                    default:
                        break;
                }
            }
            finally
            {
                defferal.Complete();
            }
        }


        public async Task ReadStream(StreamSocket socket)
        {
            using (var reader = new DataReader(socket.InputStream))
            {
                reader.InputStreamOptions = InputStreamOptions.Partial;

                // 受信したデータをバッファに読み込む
                string _LastRecieveXml = "";
                uint count = await reader.LoadAsync(512);
                while (reader.UnconsumedBufferLength > 0)
                {
                    var str = reader.ReadString(reader.UnconsumedBufferLength);

                    await reader.LoadAsync(512);

                    var text = _LastRecieveXml + str;
                    // バッファに詰め込まれた文字列を解析する
                    var xmlStrings = text.ToString()
                        .Split('\0')
                        .ToArray();

                    _LastRecieveXml = xmlStrings.Last();

                    foreach (var xmlString in xmlStrings.Take(xmlStrings.Length - 1))
                    {
                        try
                        {
                            ParseAlertXmlString(xmlString);
                        }
                        catch
                        {
                            return;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(_LastRecieveXml)) { return; }
                }

                reader.DetachStream();
            }
        }

        private void ParseAlertXmlString(string xmlString)
        {
            var xml = new XmlDocument();
            xml.LoadXml(xmlString);

            var root = xml?.FirstChild;

            if (root?.Name == "chat")
            {
                var Ids = root.InnerText.Split(',');
                var threadId = root.Attributes["thread"];


                var type = (NiconicoAlertServiceType)int.Parse(threadId.Value);

                switch (type)
                {
                    case NiconicoAlertServiceType.Live:
                        OnLiveRecieved(new NicoLiveAlertEventArgs()
                        {
                            Id = Ids.ElementAt(0),
                            CommunityId = Ids.ElementAtOrDefault(1),
                            BroadcasterUserId = Ids.ElementAtOrDefault(2)
                        });
                        break;
                    case NiconicoAlertServiceType.Video:
                        OnVideoRecieved(new NicoVideoAlertEventArgs()
                        {
                            Id = Ids.ElementAt(0),
                            UserId = Ids.ElementAtOrDefault(1)
                        });
                        break;
                    case NiconicoAlertServiceType.Seiga:
                        OnSeigaRecieved(new NicoVideoAlertEventArgs()
                        {
                            Id = Ids.ElementAt(0),
                            UserId = Ids.ElementAt(1)
                        });
                        break;
                    default:
                        break;
                }
                
            }
        }


        protected virtual void OnLiveRecieved(NicoLiveAlertEventArgs args) { }
        protected virtual void OnVideoRecieved(NicoVideoAlertEventArgs args) { }
        protected virtual void OnSeigaRecieved(NicoVideoAlertEventArgs args) { }
    }
}
