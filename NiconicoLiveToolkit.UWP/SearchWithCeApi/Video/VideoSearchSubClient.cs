using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.SearchWithCeApi.Video
{
    public sealed class VideoSearchSubClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _option;

        internal VideoSearchSubClient(NiconicoContext context, JsonSerializerOptions defaultOptions)
        {
            _context = context;
            _option = defaultOptions;
        }


        public async Task<VideoIdSearchSingleResponse> IdSearchAsync(VideoId videoId)
        {
            string url = $"{NiconicoUrls.CeNicoApiV1Url}video.info?v={videoId}&__format=json";
            var res = await _context.GetJsonAsAsync<VideoSingleIdSearchResponseContainer>(url, _option);
            return res.Response;
        }

        public async Task<VideoIdSearchResponse> IdSearchAsync(IEnumerable<VideoId> videoIdList)
        {
            string url = $"{NiconicoUrls.CeNicoApiV1Url}video.array?v={string.Join(Uri.EscapeDataString(","), videoIdList)}&__format=json";
            var res = await _context.GetJsonAsAsync<VideoIdSearchResponseContainer>(url, _option);
            return res.Response;
        }

        public async Task<VideoListingResponse> KeywordSearchAsync(
			string keyword
			, int from
			, int limit
			, VideoSortKey? sort = null
			, VideoSortOrder? order = null
			)
		{
            var dict = new NameValueCollection()
            {
                { "str", keyword },
                { "from", from.ToString() },
                { "limit", limit.ToString() },
            };

            if (order.HasValue)
            {
                dict.Add("order", order.Value.GetDescription());
            }
            if (sort.HasValue)
            {
                dict.Add("sort", sort.Value.GetDescription());
            }

            await _context.WaitPageAccessAsync();

            var url = new StringBuilder($"{NiconicoUrls.CeNicoApiV1Url}video.search")
                .AppendQueryString(dict)
                .ToString();

            var res = await _context.GetJsonAsAsync<VideoListingResponseContainer>(url, _option);
            return res.Response;
        }

		public async Task<VideoListingResponse> TagSearchAsync(
			string tag
			, int from
			, int limit
			, VideoSortKey? sort = null
			, VideoSortOrder? order = null
			)
		{
            var dict = new NameValueCollection()
            {
                { "tag", tag },
                { "from", from.ToString() },
                { "limit", limit.ToString() },
            };

            if (order.HasValue)
            {
                dict.Add("order", order.Value.GetDescription());
            }
            if (sort.HasValue)
            {
                dict.Add("sort", sort.Value.GetDescription());
            }

            await _context.WaitPageAccessAsync();

            var url = new StringBuilder($"{NiconicoUrls.CeNicoApiV1Url}tag.search")
                .AppendQueryString(dict)
                .ToString();

            var res = await _context.GetJsonAsAsync<VideoListingResponseContainer>(url, _option);
            return res.Response;
        }
	}

}
