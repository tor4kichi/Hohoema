using NiconicoToolkit.Live;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
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

        private readonly JsonSerializerOptions _liveRecommendOptions;

        public RecommendClient(NiconicoContext context, JsonSerializerOptions defaultOptions)
        {
            _context = context;
            _options = new JsonSerializerOptions(defaultOptions)
            {
                Converters =
                {
                    new RecommendItemContentConverter(),
                }
            };

            _liveRecommendOptions = NiconicoContext.DefaultOptionsSnakeCase;
        }

        internal static class Urls
        {
            public const string NvApiV1RecommendApiUrl = $"{NiconicoUrls.NvApiV1Url}recommend";

            public const string LiveFrontV1ApiUrl = $"{NiconicoUrls.NicoLive2PageUrl}/front/api/v1/";
            public const string LiveRecommendApiUrl = $"{LiveFrontV1ApiUrl}recommend-contents";
        }

        public Task<VideoRecommendResponse> GetVideoRecommendForNotChannelAsync(string videoId)
        {
            string url = $"{Urls.NvApiV1RecommendApiUrl}?recipeId=video_watch_recommendation&videoId={videoId}&site=nicovideo&_frontendId=6&_frontendVersion=0";
            return _context.GetJsonAsAsync<VideoRecommendResponse>(url, _options);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="videoId">so始まりの動画ID</param>
        /// <param name="channelId">ch始まりのチャンネルID</param>
        /// <param name="tags">未エンコードのタグ文字列のリスト</param>
        /// <returns></returns>
        public Task<VideoRecommendResponse> GetVideoRecommendForChannelAsync(string videoId, string channelId, IEnumerable<string> tags)
        {
            var tagsEncoded = Uri.EscapeDataString(string.Join(' ', tags));
            string url = $"{Urls.NvApiV1RecommendApiUrl}?recipeId=video_channel_watch_recommendation&videoId={videoId}&channelId={channelId}&tags={tagsEncoded}&site=nicovideo&_frontendId=6&_frontendVersion=0";
            return _context.GetJsonAsAsync<VideoRecommendResponse>(url, _options);
        }

        

        public Task<LiveRecommendResponse> GetLiveRecommendForUserAsync(LiveId liveId, UserId userId, IEnumerable<string> tags = null) 
        {
            // https://live.nicovideo.jp/watch/lv332342875
            // https://live2.nicovideo.jp/front/api/v1/recommend-contents?user_id=91190464&frontend_id=9&tags=アニメ&site=nicolive&content_meta=true&live_id=lv332342875&recipe=live_watch_user&v=1

            var dict = new NameValueCollection()
            {
                { "recipe", "live_watch_user" },
                { "frontend_id", "9" },
                { "content_meta", "true" },
                { "user_id", userId },
                { "live_id", liveId },
                { "site", "nicolive" },
                { "v", "1" },
            };

            //recipe=live_watch_channel&v=1
            var url = new StringBuilder(Urls.LiveRecommendApiUrl)
                .AppendQueryString(dict)
                .Append("&tags=")
                .AppendJoin(',', tags?.Select(x => Uri.EscapeDataString(x)) ?? Enumerable.Empty<string>())
                .ToString();

            return _context.GetJsonAsAsync<LiveRecommendResponse>(url, _liveRecommendOptions);
        }



        public Task<LiveRecommendResponse> GetLiveRecommendForChannelAsync(LiveId liveId, string channelId, IEnumerable<string> tags = null)
        {
            // https://live.nicovideo.jp/watch/lv331311774
            // https://live2.nicovideo.jp/front/api/v1/recommend-contents?channel_id=ch2647027&frontend_id=9&tags=%E3%82%A2%E3%83%8B%E3%83%A1,%E4%B8%8A%E6%98%A0%E4%BC%9A,2021%E5%86%AC%E3%82%A2%E3%83%8B%E3%83%A1,%E8%9C%98%E8%9B%9B%E3%81%A7%E3%81%99%E3%81%8C%E3%80%81%E3%81%AA%E3%81%AB%E3%81%8B%EF%BC%9F,%E6%82%A0%E6%9C%A8%E7%A2%A7,%E5%A0%80%E6%B1%9F%E7%9E%AC,%E6%9D%B1%E5%B1%B1%E5%A5%88%E5%A4%AE,%E7%9F%B3%E5%B7%9D%E7%95%8C%E4%BA%BA,%E5%B0%8F%E5%80%89%E5%94%AF,%E5%96%9C%E5%A4%9A%E6%9D%91%E8%8B%B1%E6%A2%A8&site=nicolive&content_meta=true&live_id=lv331311774&recipe=live_watch_channel&v=1

            var channelIdWithPrefix = ContentIdHelper.EnsurePrefixChannelId(channelId);

            var dict = new NameValueCollection()
            {
                { "recipe", "live_watch_channel" },
                { "frontend_id", "9" },
                { "content_meta", "true" },
                { "channel_id", channelIdWithPrefix  },
                { "live_id", liveId },
                { "site", "nicolive" },
                { "v", "1" },
            };

            //recipe=live_watch_channel&v=1
            var url = new StringBuilder(Urls.LiveRecommendApiUrl)
                .AppendQueryString(dict)
                .Append("&tags=")
                .AppendJoin(',', tags?.Select(x => Uri.EscapeDataString(x)) ?? Enumerable.Empty<string>())
                .ToString();

            return _context.GetJsonAsAsync<LiveRecommendResponse>(url, _liveRecommendOptions);
        }
    }


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