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

        public LiveNotifyClient(NiconicoContext context)
        {
            _context = context;

            _jsonSerializeOptions = new JsonSerializerOptions()
            {
                Converters =
                {
                    new JsonStringEnumMemberConverter(JsonSnakeCaseNamingPolicy.Instance),
                }
            };
        }

        public async Task<LiveNotifyUnreadResponse> GetUnreadLiveNotifyAsync()
        {
            return await _context.GetJsonAsAsync<LiveNotifyUnreadResponse>("https://papi.live.nicovideo.jp/api/relive/notifybox.unread");
        }

        public async Task<LiveNotifyContentResponse> GetLiveNotifyAsync(int rows = 100)
        {
            return await _context.GetJsonAsAsync<LiveNotifyContentResponse>($"https://papi.live.nicovideo.jp/api/relive/notifybox.content?rows={rows}", _jsonSerializeOptions);
        }
    }
    

    public partial class LiveNotifyUnreadResponse
    {
        [J("meta")]
        public LiveNotifyUnreadMeta Meta { get; set; }

        [J("data")]
        public LiveNotifyUnreadData Data { get; set; }
    }

    public partial class LiveNotifyUnreadData
    {
        [J("count")]
        public long Count { get; set; }

        [J("is_unread")]
        public bool IsUnread { get; set; }
    }

    public partial class LiveNotifyUnreadMeta
    {
        [J("status")]
        public long Status { get; set; }
    }



    public partial class LiveNotifyContentResponse
    {
        [J("meta")]
        public LiveNotifyMeta Meta { get; set; }

        [J("data")]
        public LiveNotifyData Data { get; set; }
    }

    public partial class LiveNotifyData
    {
        [J("notifybox_content")]
        public NotifyboxContent[] NotifyboxContent { get; set; }

        [J("total_page")]
        public long TotalPage { get; set; }
    }

    public partial class NotifyboxContent
    {
        [J("id")]
        public string Id { get; set; }

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

    public partial class LiveNotifyMeta
    {
        [J("status")]
        public long Status { get; set; }
    }
}
