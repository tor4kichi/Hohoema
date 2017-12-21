using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace Hohoema.NicoAlert
{

    public struct NicoVideoAlertEventArgs
    {
        public string Id { get; set; }
        public string UserId { get; set; }
    }
    public struct NicoLiveAlertEventArgs
    {
        public string Id { get; set; }
        public string CommunityId { get; set; }
        public string BroadcasterUserId { get; set; }
    }
    public sealed class NicoAlertClient : IDisposable
    {
        // TODO: DataWriterのDisposeは無くて問題ないか？


        // Note: 

        public event EventHandler<NicoVideoAlertEventArgs> VideoRecieved;
        public event EventHandler<NicoVideoAlertEventArgs> SeigaRecieved;
        public event EventHandler<NicoLiveAlertEventArgs> LiveRecieved;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public string Mail { get; private set; }
        public string Password { get; private set; }

        HttpClient _HttpClient = new HttpClient();

        const string NicoAlertLoginSiteUrl = "https://secure.nicovideo.jp/secure/login?site=nicoalert";
        const string AlertStatusSiteUrl = "http://alert.nicovideo.jp/front/getalertstatus";
        const string FollowListSiteUrl = "http://alert.nicovideo.jp/front/getcommunitylist";

        AlertStatesInfo _AlertStatesInfo;

        CancellationTokenSource ReadingTaskCancelSource;
        StreamSocket _StreamSocket;
        Task _ReadingTask;
        Helpers.AsyncLock _NetworkStreamLock = new Helpers.AsyncLock();


        public NicoAlertClient()
        {
            _HttpClient = new HttpClient();
        }


        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                    _StreamSocket?.Dispose();
                    _HttpClient.Dispose();
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~NiconicoAlertClient() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion


        public bool IsOpenSuccess { get; private set; }


        
        


        public async Task<bool> LoginAsync(string mail, string password)
        {
            Mail = mail;
            Password = password;

            IsOpenSuccess = false;

            var ticket = await GetNicoAlertTicketAsync();

            if (ticket == null)
            {
                // TODO: ログイン失敗のイベントを発行
            }

            var alertStatus = await GetAlertStatusAsync(ticket);

            if (alertStatus == null || alertStatus.Status != "ok")
            {
                // TODO: アラートサーバーの情報取得に失敗
            }

            _AlertStatesInfo = alertStatus;
            _ThreadIdToServiceType.Clear();

            foreach (var service in _AlertStatesInfo.Services.Service)
            {
                if (Enum.TryParse(service.Thread, out NiconicoAlertServiceType serviceType))
                {
                    _ThreadIdToServiceType.Add(service.Thread, serviceType);
                }
            }
            IsOpenSuccess = true;

            return IsOpenSuccess;
        }

        private async Task<T> GetXmlDataAsync<T>(string url, params KeyValuePair<string, string>[] parameters)
        {
            var content = new HttpFormUrlEncodedContent(parameters);

            var res = await _HttpClient.PostAsync(new Uri(url), content);

            if (res.IsSuccessStatusCode)
            {
                var alertstatusRes = await res.Content.ReadAsStringAsync();
                var serializer = new XmlSerializer(typeof(T));
                using (var textReader = System.Xml.XmlReader.Create(new System.IO.StringReader(alertstatusRes)))
                {
                    if (serializer.CanDeserialize(textReader))
                    {
                        return (T)serializer.Deserialize(textReader);
                    }
                }
            }

            return default(T);
        }

        public async Task<FollowInfo> GetFollowsAsync()
        {
            if (_AlertStatesInfo == null)
            {
                throw new Exception();
            }

            return await GetXmlDataAsync<FollowInfo>(FollowListSiteUrl,
                new KeyValuePair<string, string>("user_hash", _AlertStatesInfo.UserHash),
                new KeyValuePair<string, string>("user_id", _AlertStatesInfo.UserId)
                );
        }

        private async Task<AlertStatesInfo> GetAlertStatusAsync(string ticket)
        {
            return await GetXmlDataAsync<NicoAlert.AlertStatesInfo>(AlertStatusSiteUrl,
                new KeyValuePair<string, string>("ticket", ticket)
                );
        }

        private async Task<string> GetNicoAlertTicketAsync()
        {
            var res = await GetXmlDataAsync<NicoAlert.NicoVideoUserResponse>(NicoAlertLoginSiteUrl,
                new KeyValuePair<string, string>("mail", Mail),
                new KeyValuePair<string, string>("password", Password)
                );

            return res?.Ticket;
        }

        Dictionary<string, NiconicoAlertServiceType> _ThreadIdToServiceType = new Dictionary<string, NiconicoAlertServiceType>();

        public Task ConnectAlertWebScoketServerAsync()
        {
            return ConnectAlertWebScoketServerAsync((NiconicoAlertServiceType[])Enum.GetValues(typeof(NiconicoAlertServiceType)));
        }


        const string NicoAlertSocketServiceName = "hohoema_nico_alert";

        public async Task ConnectAlertWebScoketServerAsync(params NiconicoAlertServiceType[] recieveAlertTypes)
        {
            if (recieveAlertTypes == null || recieveAlertTypes.Length == 0)
            {
                throw new ArgumentException("recieveAlertTypes is empty.");
            }

            
            var hostName = new HostName(_AlertStatesInfo.Ms.Addr);

            try
            {
                _StreamSocket = new StreamSocket();
                await _StreamSocket.ConnectAsync(hostName, _AlertStatesInfo.Ms.Port);

                using (var messageWriter = new StreamWriter(_StreamSocket.OutputStream.AsStreamForWrite(), Encoding.UTF8))
                {
                    foreach (var serviceType in recieveAlertTypes)
                    {
                        var serviceTypeId = ((long)serviceType).ToString();
                        var serviceThreadId = _AlertStatesInfo.Services.Service.FirstOrDefault(x => x.Thread == serviceTypeId).Thread;
                        await messageWriter.WriteAsync($"<thread thread=\"{serviceThreadId}\" version=\"20061206\" res_from=\"-1\"/>\0");
                    }

                    await messageWriter.FlushAsync();
                }
            }
            catch
            {
                await _StreamSocket.CancelIOAsync();

                throw;
            }

            Connected?.Invoke(this, EventArgs.Empty);


            ReadingTaskCancelSource = new CancellationTokenSource();
            _ReadingTask = DataRecivingTask(ReadingTaskCancelSource.Token);
        }


        bool _NowReading = false;
        string _LastRecieveXml = "";
        private async Task DataRecivingTask(CancellationToken token)
        {
            if (_NowReading) { return; }
            Debug.WriteLine("start alert recieve.");

            using (var reader = new DataReader(_StreamSocket.InputStream))
            {
                reader.InputStreamOptions = InputStreamOptions.Partial;

                // 受信したデータをバッファに読み込む

                uint count = await reader.LoadAsync(512);
                while (reader.UnconsumedBufferLength > 0)
                {
                    token.ThrowIfCancellationRequested();

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
                        Debug.Write($"recieve data -> ");

                        try
                        {
                            ParseAlertXmlString(xmlString);
                            Debug.WriteLine($" -> end");
                        }
                        catch
                        {
                            Debug.WriteLine($" -> FAILED!");
                            Debug.WriteLine(xmlString);
                        }
                    }
                }

                reader.DetachStream();

                _NowReading = false;
            }

            Debug.WriteLine("exit alert recieve.");
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

                if (_ThreadIdToServiceType.TryGetValue(threadId.Value, out var type))
                {
                    switch (type)
                    {
                        case NiconicoAlertServiceType.Live:
                            LiveRecieved?.Invoke(this, new NicoLiveAlertEventArgs()
                            {
                                Id = Ids.ElementAt(0),
                                CommunityId = Ids.ElementAtOrDefault(1),
                                BroadcasterUserId = Ids.ElementAtOrDefault(2)
                            });
                            break;
                        case NiconicoAlertServiceType.Video:
                            VideoRecieved?.Invoke(this, new NicoVideoAlertEventArgs()
                            {
                                Id = Ids.ElementAt(0),
                                UserId = Ids.ElementAtOrDefault(1)
                            });
                            break;
                        case NiconicoAlertServiceType.Seiga:
                            SeigaRecieved?.Invoke(this, new NicoVideoAlertEventArgs()
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
        }

        public async void Disconnect()
        {
            using (var releaser = await _NetworkStreamLock.LockAsync())
            {
                ReadingTaskCancelSource?.Cancel();

                await Task.Delay(250);

                ReadingTaskCancelSource?.Dispose();

                if (_StreamSocket != null)
                {
                    await _StreamSocket.CancelIOAsync();
                    _StreamSocket.Dispose();
                    _StreamSocket = null;
                }
            }

            Disconnected?.Invoke(this, EventArgs.Empty);
        }


    }


    public enum NiconicoAlertServiceType
    {
        Live  = 1000000001,
        Video = 1000000002,
        Seiga = 1000000003,
    }
}
