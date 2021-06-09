using NiconicoToolkit.Live.Notify;
using NiconicoToolkit.Live.Search;
using NiconicoToolkit.Live.WatchPageProp;
using NiconicoToolkit.Live.WatchSession;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NiconicoToolkit.Live
{
    public sealed class LiveClient
    {
        private readonly NiconicoContext _context;

        internal LiveClient(NiconicoContext context)
        {
            _context = context;
            Search = new LiveSearchClient(context);
            CasApi = new Cas.CasLiveClient(context);
            LiveNotify = new LiveNotifyClient(context);
        }

        public LiveSearchClient Search { get; }
        public Cas.CasLiveClient CasApi { get; }
        public LiveNotifyClient LiveNotify { get; }

        public static string MakeLiveWatchPageUrl(string liveId)
        {
            return $"{LiveWatchPageUrl}{liveId}";
        }

        const string LiveWatchPageUrl = "https://live2.nicovideo.jp/watch/";
        public async Task<LiveWatchPageDataProp> GetLiveWatchPageDataPropAsync(string liveId, CancellationToken ct = default)
        {
            await _context.WaitPageAccessAsync();

            var html = await _context.GetStringAsync(MakeLiveWatchPageUrl(liveId));
            var scriptIdPosition = html.IndexOf("id=\"embedded-data\"");
            if (scriptIdPosition < 0) { throw new Exception(); }

            const string datapropsString = "data-props=\"";
            var dataPropsAttrHeadPosition = html.IndexOf(datapropsString, scriptIdPosition);
            if (dataPropsAttrHeadPosition < 0) { throw new Exception(); }

            var dataPropsAttrStartPosition = dataPropsAttrHeadPosition + datapropsString.Length;
            var dataPropsAttrEndPosition = html.IndexOf("\"></script>", dataPropsAttrStartPosition);
            var json = html.Substring(dataPropsAttrStartPosition, dataPropsAttrEndPosition - dataPropsAttrStartPosition);
            var decodedJson = WebUtility.HtmlDecode(json);
            var invalidJsonTokenRemoved = decodedJson.Replace("\"type\":\"\",", "");
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Converters = 
                {
                    new JsonStringEnumMemberConverter(JsonSnakeCaseNamingPolicy.Instance),
                    new JsonStringEnumMemberConverter(JsonNamingPolicy.CamelCase),
                    new LongToStringConverter()
                }
            };

            Debug.WriteLine(decodedJson);

            return JsonDeserializeHelper.Deserialize<LiveWatchPageDataProp>(invalidJsonTokenRemoved, options);
        }        


        public static Live2WatchSession CreateWatchSession(LiveWatchPageDataProp prop)
        {
            bool isWatchTimeshift =
                prop.Program.Status == ProgramLiveStatus.ENDED
                && (prop.ProgramTimeshiftWatch?.Condition.NeedReservation ?? false) 
                ;
            return new Live2WatchSession(prop.Site.Relive.WebSocketUrl, isWatchTimeshift);
        }


        public static string MakeSeekedHLSUri(string hlsUri, TimeSpan position)
        {
            if (position > TimeSpan.FromSeconds(1))
            {
                return hlsUri += $"&start={position.TotalSeconds.ToString("F2")}";
            }
            else
            {
                return hlsUri;
            }
        }
    }
}
