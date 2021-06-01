using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace NiconicoToolkit.Activity.VideoWatchHistory
{
    public sealed class VideoWatchHisotrySubClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _options;

        public VideoWatchHisotrySubClient(NiconicoContext context)
        {
            _context = context;


            _options = new JsonSerializerOptions()
            {
                Converters =
                {
                    new JsonStringEnumMemberConverter(),
                }
            };
        }

        public const string WatchHitoryApi = "https://nvapi.nicovideo.jp/v1/users/me/watch/history";

        public Task<VideoWatchHistory> GetWatchHistoryAsync(int page, int pageSize)
        {
            var url = new StringBuilder(WatchHitoryApi)
                .AppendQueryString(new NameValueCollection()
                {
                    { "page", (page+1).ToString() },
                    { "pageSize", (pageSize).ToString() },
                })
                .ToString();

            return _context.GetJsonAsAsync<VideoWatchHistory>(url, _options);
        }


        public Task<VideoWatchHistoryDeleteResult> DeleteWatchHistoriesAsync(string target)
        {
            var url = new StringBuilder(WatchHitoryApi)
                .AppendQueryString(new NameValueCollection()
                {
                    { "target", target },
                })
                .ToString();

            return _context.DeleteJsonAsAsync<VideoWatchHistoryDeleteResult>(url, _options);
        }

        public Task<VideoWatchHistoryDeleteResult> DeleteWatchHistoriesAsync(IEnumerable<string> targets)
        {
            var url = new StringBuilder(WatchHitoryApi)
                .AppendQueryString(new NameValueCollection()
                {
                    { "target", string.Join(',', targets) },
                })
                .ToString();

            return _context.DeleteJsonAsAsync<VideoWatchHistoryDeleteResult>(url, _options);
        }


        public Task<VideoWatchHistoryDeleteResult> DeleteAllWatchHistoriesAsync()
        {
            var url = new StringBuilder(WatchHitoryApi)
                .AppendQueryString(new NameValueCollection()
                {
                    { "target", "all" },
                })
                .ToString();

            return _context.DeleteJsonAsAsync<VideoWatchHistoryDeleteResult>(url, _options);
        }
    }

    public class VideoWatchHistoryDeleteResult
    {
        [JsonPropertyName("meta")]
        public VideoWatchHistory.VideoWatchHistoryMeta Meta { get; set; }

        public bool IsOK => Meta.IsOK;
    }

    public class VideoWatchHistory
    {
        [JsonPropertyName("meta")]
        public VideoWatchHistoryMeta Meta { get; set; }

        [JsonPropertyName("data")]
        public VideoWatchHistoryData Data { get; set; }


        public class VideoWatchHistoryData
        {
            [JsonPropertyName("totalCount")]
            public long TotalCount { get; set; }

            [JsonPropertyName("items")]
            public VideoWatchHistoryItem[] Items { get; set; }
        }

        public class VideoWatchHistoryItem
        {
            [JsonPropertyName("watchId")]
            public string WatchId { get; set; }

            [JsonPropertyName("frontendId")]
            public long FrontendId { get; set; }

            [JsonPropertyName("views")]
            public long Views { get; set; }

            [JsonPropertyName("lastViewedAt")]
            public DateTimeOffset LastViewedAt { get; set; }

            [JsonPropertyName("playbackPosition")]
            public long PlaybackPosition { get; set; }

            [JsonPropertyName("video")]
            public NvapiVideoItem Video { get; set; }
        }

        
        public class VideoWatchHistoryMeta
        {
            [JsonPropertyName("status")]
            public long Status { get; set; }


            public bool IsOK => Status == 200;
        }

    }

    
}
