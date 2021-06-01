using NiconicoToolkit.Mylist;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Recommend
{
    public sealed class RecommendClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _options;

        public RecommendClient(NiconicoContext context)
        {
            _context = context;
            _options = new JsonSerializerOptions()
            {
                Converters =
                {
                    new JsonStringEnumMemberConverter(),
                    new RecommendItemContentConverter(),
                }
            };
        }

        public Task<VideoRecommendResponse> GetVideoReccommendAsync(string videoId)
        {
            string url = $"https://nvapi.nicovideo.jp/v1/recommend?recipeId=video_watch_recommendation&videoId={videoId}&site=nicovideo&_frontendId=6&_frontendVersion=0";
            return _context.GetJsonAsAsync<VideoRecommendResponse>(url, _options);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="videoId">so始まりの動画ID</param>
        /// <param name="channelId">ch始まりのチャンネルID</param>
        /// <param name="tags">未エンコードのタグ文字列のリスト</param>
        /// <returns></returns>
        public Task<VideoRecommendResponse> GetChannelVideoReccommendAsync(string videoId, string channelId, IEnumerable<string> tags)
        {
            var tagsEncoded = Uri.EscapeDataString(string.Join(' ', tags));
            string url = $"https://nvapi.nicovideo.jp/v1/recommend?recipeId=video_channel_watch_recommendation&videoId={videoId}&channelId={channelId}&tags={tagsEncoded}&site=nicovideo&_frontendId=6&_frontendVersion=0";
            return _context.GetJsonAsAsync<VideoRecommendResponse>(url, _options);
        }
    }

    public partial class ReccomendRecipe
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("meta")]
        public object Meta { get; set; }
    }

    public partial class VideoRecommendMeta
    {
        [JsonPropertyName("status")]
        public long Status { get; set; }


        public bool IsOK => Status == 200;
    }
    public class VideoRecommendResponse
    {
        [JsonPropertyName("meta")]
        public VideoRecommendMeta Meta { get; set; }

        [JsonPropertyName("data")]
        public VideoRecommendData Data { get; set; }
    }

    public class VideoRecommendData
    {
        [JsonPropertyName("recipe")]
        public ReccomendRecipe Recipe { get; set; }

        [JsonPropertyName("recommendId")]
        public string RecommendId { get; set; }

        [JsonPropertyName("items")]
        [JsonConverter(typeof(RecommendItemContentConverter))]
        public VideoReccomendItem[] Items { get; set; }
    }

    public class VideoReccomendItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("contentType")]
        public RecommendContentType ContentType { get; set; }

        [JsonPropertyName("recommendType")]
        public RecommendType RecommendType { get; set; }

        [JsonPropertyName("content")]
        //public object Content { get; set; }

        public NvapiMylistItem ContentAsMylist { get;set; }

        public NvapiVideoItem ContentAsVideo { get; set; }
    }

    public enum RecommendContentType { Mylist, Video };

    public enum RecommendType { Recommend, TkasF, Search };


    public class RecommendItemContentConverter : JsonConverter<VideoReccomendItem[]>
    {
        private static readonly byte[] s_idUtf8 = Encoding.UTF8.GetBytes("id");
        private static readonly byte[] s_contentTypeUtf8 = Encoding.UTF8.GetBytes("contentType");
        private static readonly byte[] s_recommendTypeUtf8 = Encoding.UTF8.GetBytes("recommendType");
        private static readonly byte[] s_contentUtf8 = Encoding.UTF8.GetBytes("content");

        public override VideoReccomendItem[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray) { throw new JsonException(); }

            List<VideoReccomendItem> items = new();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.StartObject) { throw new JsonException(); }
                
                VideoReccomendItem item = new VideoReccomendItem();

                for (int i = 0; i < 4; i++)
                {
                    if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName) { throw new JsonException(); }
                    
                    if (reader.ValueTextEquals(s_idUtf8))
                    {
                        if (!reader.Read() || reader.TokenType != JsonTokenType.String) { throw new JsonException(); }
                        item.Id = reader.GetString();
                    }
                    else if (reader.ValueTextEquals(s_contentTypeUtf8))
                    {
                        if (!reader.Read() || reader.TokenType != JsonTokenType.String) { throw new JsonException(); }
                        item.ContentType = JsonSerializer.Deserialize<RecommendContentType>(ref reader, options);
                    }
                    else if (reader.ValueTextEquals(s_recommendTypeUtf8))
                    {
                        if (!reader.Read() || reader.TokenType != JsonTokenType.String) { throw new JsonException(); }
                        item.RecommendType = JsonSerializer.Deserialize<RecommendType>(ref reader, options);

                    }
                    else if (reader.ValueTextEquals(s_contentUtf8))
                    {
                        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) { throw new JsonException(); }
                        switch (item.ContentType)
                        {
                            case RecommendContentType.Mylist: item.ContentAsMylist = JsonSerializer.Deserialize<NvapiMylistItem>(ref reader, options); break;
                            case RecommendContentType.Video: item.ContentAsVideo = JsonSerializer.Deserialize<NvapiVideoItem>(ref reader, options); break;
                            default: throw new JsonException();
                        };
                    }
                }

                items.Add(item);

                if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject) { throw new JsonException(); }
            }

            return items.ToArray();
        }

        public override void Write(Utf8JsonWriter writer, VideoReccomendItem[] value, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }
    }


}
