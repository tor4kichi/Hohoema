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
            public Item[] Items { get; set; }
        }

        public class Item
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
            public Video Video { get; set; }
        }

        public class Video
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("registeredAt")]
            public DateTimeOffset RegisteredAt { get; set; }

            [JsonPropertyName("count")]
            public Count Count { get; set; }

            [JsonPropertyName("thumbnail")]
            public Thumbnail Thumbnail { get; set; }

            [JsonPropertyName("duration")]
            public long Duration { get; set; }

            [JsonPropertyName("shortDescription")]
            public string ShortDescription { get; set; }

            [JsonPropertyName("latestCommentSummary")]
            public string LatestCommentSummary { get; set; }

            [JsonPropertyName("isChannelVideo")]
            public bool IsChannelVideo { get; set; }

            [JsonPropertyName("isPaymentRequired")]
            public bool IsPaymentRequired { get; set; }

            [JsonPropertyName("playbackPosition")]
            public long PlaybackPosition { get; set; }

            [JsonPropertyName("owner")]
            public Owner Owner { get; set; }

            [JsonPropertyName("requireSensitiveMasking")]
            public bool RequireSensitiveMasking { get; set; }

            [JsonPropertyName("9d091f87")]
            public bool The9D091F87 { get; set; }

            [JsonPropertyName("acf68865")]
            public bool Acf68865 { get; set; }
        }

        public class Count
        {
            [JsonPropertyName("view")]
            public int View { get; set; }

            [JsonPropertyName("comment")]
            public int Comment { get; set; }

            [JsonPropertyName("mylist")]
            public int Mylist { get; set; }

            [JsonPropertyName("like")]
            public int Like { get; set; }
        }

        public class Owner
        {
            [JsonPropertyName("ownerType")]
            public OwnerType OwnerType { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("iconUrl")]
            public Uri IconUrl { get; set; }
        }

        public class Thumbnail
        {
            [JsonPropertyName("url")]
            public Uri Url { get; set; }

            [JsonPropertyName("middleUrl")]
            public Uri MiddleUrl { get; set; }

            [JsonPropertyName("largeUrl")]
            public Uri LargeUrl { get; set; }

            [JsonPropertyName("listingUrl")]
            public Uri ListingUrl { get; set; }

            [JsonPropertyName("nHdUrl")]
            public Uri NHdUrl { get; set; }
        }

        public class VideoWatchHistoryMeta
        {
            [JsonPropertyName("status")]
            public long Status { get; set; }


            public bool IsOK => Status == 200;
        }

        public enum TypeEnum { Essential };

        public enum OwnerType { User, Channel };
    }
}
