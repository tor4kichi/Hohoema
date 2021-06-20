using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Live.Notify
{
    using J = JsonPropertyNameAttribute;

    public sealed class LiveNotifyClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _jsonSerializeOptions;

        public LiveNotifyClient(NiconicoContext context, JsonSerializerOptions defaultOptions)
        {
            _context = context;

            _jsonSerializeOptions = new JsonSerializerOptions(defaultOptions)
            {
                Converters =
                {
                    new JsonStringEnumMemberConverter(JsonSnakeCaseNamingPolicy.Instance),
                }
            };
        }


        internal static class Urls
        {
            public const string P_LiveApiUrl = "https://papi.live.nicovideo.jp/api/";

            public const string P_LiveNotifyboxApiUrl = $"{P_LiveApiUrl}relive/notifybox";
            public const string P_LiveNotifyboxUnreadApiUrl = $"{P_LiveNotifyboxApiUrl}.unread";
            
        }

        public async Task<LiveNotifyUnreadResponse> GetUnreadLiveNotifyAsync()
        {
            return await _context.GetJsonAsAsync<LiveNotifyUnreadResponse>(Urls.P_LiveNotifyboxUnreadApiUrl);
        }

        public async Task<LiveNotifyContentResponse> GetLiveNotifyAsync(int rows = 100)
        {
            return await _context.GetJsonAsAsync<LiveNotifyContentResponse>($"{Urls.P_LiveNotifyboxApiUrl}.content?rows={rows}", _jsonSerializeOptions);
        }
    }
    

    public sealed class LiveNotifyUnreadResponse : ResponseWithMeta
    {
        [J("data")]
        public LiveNotifyUnreadData Data { get; set; }
    }

    public sealed class LiveNotifyUnreadData
    {
        [J("count")]
        public long Count { get; set; }

        [J("is_unread")]
        public bool IsUnread { get; set; }
    }


    public sealed class LiveNotifyContentResponse : ResponseWithMeta
    {
        [J("data")]
        public LiveNotifyData Data { get; set; }
    }

    public sealed class LiveNotifyData
    {
        [J("notifybox_content")]
        public NotifyboxContent[] NotifyboxContent { get; set; }

        [J("total_page")]
        public long TotalPage { get; set; }
    }

    public sealed class NotifyboxContent
    {
        [J("id")]
        public LiveId Id { get; set; }

        [J("title")]
        public string Title { get; set; }

        [J("thumbnail_url")]
        public Uri ThumbnailUrl { get; set; }

        [J("thumbnail_link_url")]
        public Uri ThumbnailLinkUrl { get; set; }

        [J("community_name")]
        public string CommunityName { get; set; }

        [J("elapsed_time")]
        public long ElapsedTime { get; set; }

        [J("provider_type")]
        public ProviderType ProviderType { get; set; }
    }
}
