using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.SearchWithCeApi.Mylist
{
    public sealed class MylistSearchClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _defaultOptions;

        internal MylistSearchClient(NiconicoContext context, JsonSerializerOptions defaultOptions)
        {
            _context = context;
            _defaultOptions = defaultOptions;
        }


		public async Task<MylistSearchResponse> SearchMylistAsync(string keyword, int from = 0, int limit = 25, MylistSearchSortKey? sortKey = null, MylistSearchSortOrder? sortOrder = null)
        {
			var dict = new NameValueCollection()
			{
				{ "str", keyword },
				{ "from", from.ToString() },
				{ "limit", limit.ToString() }
			};

			dict.AddEnumIfNotNullWithDescription("sort", sortKey);
			dict.AddEnumIfNotNullWithDescription("order", sortOrder);

			var url = new StringBuilder(NiconicoUrls.CeNicoApiV1Url)
				.Append("mylist.search")
				.AppendQueryString(dict)
				.ToString();

			var res = await _context.GetJsonAsAsync<MylistSearchResponseContainer>(url, _defaultOptions);
			return res?.Response;
		}
    }

    public enum MylistSearchSortKey
    {
		[Description("t")]
		CreateTime,
		
		[Description("a")]
		Title,
		
		[Description("c")]
		MylistComment,

		[Description("f")]
		FirstRetrieve,

		[Description("v")]
		ViewCount,

		[Description("n")]
		NewComment,

		[Description("r")]
		CommentCount,

		[Description("m")]
		UpdateTime,

		[Description("l")]
		Length,
	}

	public enum MylistSearchSortOrder
	{
		[Description("d")]
		Desc,

		[Description("a")]
		Asc,
	}


	public class MylistSearchResponseContainer : CeApiResponseContainerBase<MylistSearchResponse>
	{
	}

	public class MylistSearchResponse : CeApiResponseBase
	{

		[JsonPropertyName("total_count")]
		[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
		public int TotalCount { get; set; }

		[JsonPropertyName("data_count")]
		[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
		public int DataCount { get; set; }

		[JsonPropertyName("mylistgroup")]
		[JsonConverter(typeof(SingleOrArrayConverter<List<MylistGroup>, MylistGroup>))]
		public List<MylistGroup> MylistGroupItems { get; set; }

	}

	public class MylistGroup
	{
		[JsonPropertyName("id")]
		public string Id { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("description")]
		public string Description { get; set; }

		[JsonPropertyName("thread_ids")]
		public string ThreadIds { get; set; }

		[JsonPropertyName("item")]
		[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
		public int ItemsCount { get; set; }

		[JsonPropertyName("update_time")]
		public DateTimeOffset UpdateTime { get; set; }

		[JsonPropertyName("video_info")]
		[JsonConverter(typeof(SingleOrArrayConverter<List<Video.VideoInfo>, Video.VideoInfo>))]
		public List<Video.VideoInfo> VideoInfoItems { get; set; }
	}

	

}
