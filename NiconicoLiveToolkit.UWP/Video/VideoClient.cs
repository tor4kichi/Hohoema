using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video
{
    public sealed class VideoClient
    {
        private readonly NiconicoContext _context;

        JsonSerializerOptions _videoInfoSerializerOption;

        public Ranking.VideoRankinguSubClient Ranking { get; }

        internal VideoClient(NiconicoContext context)
        {
            _context = context;
            Ranking = new Ranking.VideoRankinguSubClient(context);

            _videoInfoSerializerOption = new JsonSerializerOptions()
            {
                Converters =
                {
                    new JsonStringEnumMemberConverter()
                }
            };
        }

        public async Task<NicovideoVideoResponse> GetVideoInfoAsync(string videoId)
        {
            string url = $"http://api.ce.nicovideo.jp/nicoapi/v1/video.info?v={videoId}&__format=json";
            var res = await _context.GetJsonAsAsync<NicoVideoInfoResponseContainer>(url, _videoInfoSerializerOption);
            return res.NicovideoVideoResponse;
        }

        public async Task<NicovideoVideoManyResponse> GetVideoInfoManyAsync(IEnumerable<string> videoIdList)
        {
            string url = $"http://api.ce.nicovideo.jp/nicoapi/v1/video.array?v={string.Join(Uri.EscapeDataString(","), videoIdList)}&__format=json";
            var res = await _context.GetJsonAsAsync<NicoVideoInfoManyResponseContainer>(url, _videoInfoSerializerOption);
            return res.NicovideoVideoResponse;
        }
    }

    public partial class NicoVideoInfoManyResponseContainer
    {
        [JsonPropertyName("niconico_response")]
        public NicovideoVideoManyResponse NicovideoVideoResponse { get; set; }
    }

    public class NicovideoVideoManyResponse
    {
        [JsonPropertyName("video_info")]
        [JsonConverter(typeof(SingleOrArrayConverter<List<NicovideoVideoItem>, NicovideoVideoItem>))]
        public List<NicovideoVideoItem> Videos { get; set; }

        [JsonPropertyName("@status")]
        public string Status { get; set; }


        public bool IsOK => Status == "ok";
    }

    public partial class NicovideoVideoItem
    {
        [JsonPropertyName("video")]
        public Video Video { get; set; }

        [JsonPropertyName("thread")]
        public Thread Thread { get; set; }
    }


    public partial class NicoVideoInfoResponseContainer
    {
        [JsonPropertyName("niconico_response")]
        public NicovideoVideoResponse NicovideoVideoResponse { get; set; }
    }

    public partial class NicovideoVideoResponse
    {
        [JsonPropertyName("video")]
        public Video Video { get; set; }

        [JsonPropertyName("thread")]
        public Thread Thread { get; set; }

        [JsonPropertyName("tags")]
        public Tags Tags { get; set; }

        [JsonPropertyName("@status")]
        public string Status { get; set; }

        [JsonIgnore]
        public bool IsOK => Status == "ok";
    }

    public partial class Tags
    {
        [JsonPropertyName("tag_info")]
        [JsonConverter(typeof(System.Text.Json.Serialization.SingleOrArrayConverter<List<TagInfo>, TagInfo>))]
        public List<TagInfo> TagInfo { get; set; }
    }

    public partial class TagInfo
    {
        [JsonPropertyName("tag")]
        public string Tag { get; set; }

        [JsonPropertyName("area")]
        public string Area { get; set; }
    }

    public partial class Thread
    {
        [JsonPropertyName("id")]
        [JsonConverter(typeof(System.Text.Json.Serialization.LongToStringConverter))]
        public long Id { get; set; }

        [JsonPropertyName("num_res")]
        [JsonConverter(typeof(System.Text.Json.Serialization.LongToStringConverter))]
        public long NumRes { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("community_id")]
        public string CommunityId { get; set; }

        [JsonPropertyName("group_type")]
        public string GroupType { get; set; }
    }

    public partial class Video
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("user_id")]
        [JsonConverter(typeof(System.Text.Json.Serialization.LongToStringConverter))]
        public long UserId { get; set; }

        [JsonPropertyName("deleted")]
        [JsonConverter(typeof(System.Text.Json.Serialization.LongToStringConverter))]
        public long Deleted { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("length_in_seconds")]
        [JsonConverter(typeof(System.Text.Json.Serialization.LongToStringConverter))]
        public long LengthInSeconds { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public Uri ThumbnailUrl { get; set; }

        [JsonPropertyName("first_retrieve")]
        public DateTimeOffset FirstRetrieve { get; set; }

        [JsonPropertyName("view_counter")]
        [JsonConverter(typeof(System.Text.Json.Serialization.LongToStringConverter))]
        public long ViewCounter { get; set; }

        [JsonPropertyName("mylist_counter")]
        [JsonConverter(typeof(System.Text.Json.Serialization.LongToStringConverter))]
        public long MylistCounter { get; set; }

        [JsonPropertyName("genre")]
        public Genre Genre { get; set; }

        [JsonPropertyName("community_id")]
        public string CommunityId { get; set; }

        [JsonPropertyName("ppv_video")]
        [JsonConverter(typeof(System.Text.Json.Serialization.LongToStringConverter))]
        public long PpvVideo { get; set; }


        [JsonPropertyName("provider_type")]
        public VideoProviderType ProviderType { get; set; }

        [JsonIgnore]
        private bool IsPayRequired => PpvVideo == 1;

        /*
        [JsonIgnore]
        public VideoPermission VideoPermission
        {
            get => Permission switch
            {
                "" or "0" => VideoPermission.None,
                "1" => IsPayRequired ? VideoPermission.RequirePay : VideoPermission.RequirePremiumMember,
                "2" => VideoPermission.FreeForChannelMember,
                "3" => VideoPermission.VideoPermission_3,
                "4" => VideoPermission.MemberUnlimitedAccess,
                _ => VideoPermission.Unknown,
            };
        }
        */
    }


    public partial class Genre
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }
    }

    public partial class Options
    {
        [JsonPropertyName("@mobile")]
        [JsonConverter(typeof(System.Text.Json.Serialization.LongToStringConverter))]
        public long? Mobile { get; set; }

        [JsonPropertyName("@sun")]
        [JsonConverter(typeof(System.Text.Json.Serialization.LongToStringConverter))]
        public long? Sun { get; set; }

        [JsonPropertyName("@large_thumbnail")]
        [JsonConverter(typeof(System.Text.Json.Serialization.LongToStringConverter))]
        public long? LargeThumbnail { get; set; }

        [JsonPropertyName("@adult")]
        [JsonConverter(typeof(System.Text.Json.Serialization.LongToStringConverter))]
        public long? Adult { get; set; }
    }
}

