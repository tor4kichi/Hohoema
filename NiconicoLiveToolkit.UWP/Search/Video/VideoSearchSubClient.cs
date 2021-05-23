using AngleSharp.Html.Parser;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NiconicoToolkit.Video.Ranking;

namespace NiconicoToolkit.Search.Video
{
    public sealed class VideoSearchSubClient
    {
        private readonly NiconicoContext _context;

        public VideoSearchSubClient(NiconicoContext context)
        {
            _context = context;
        }

		internal static void ThrowExceptionIfInvalidGenre(RankingGenre genre)
        {
			if (genre is RankingGenre.All or RankingGenre.HotTopic)
			{
				throw new ArgumentException("Search can not use to Genre with 'All' or 'HotTopic'.");
			}
		}


		internal static void ThrowExceptionIfInvalidRangeDatePair(DateTime start, DateTime end)
		{
			if (start >= end)
			{
				throw new InvalidOperationException("end date is must be after start date");
			}
		}

		public const int MaxPageCount = 50;
		public const int OnePageItemsCount = 32;

		public const string KeywordSearchApi = "http://ext.nicovideo.jp/api/search/search/";
		public const string TagSearchApi = "http://ext.nicovideo.jp/api/search/tag/";


		public VideoSearchQueryBuilder CreateQueryBuilder()
		{
			return new VideoSearchQueryBuilder(this);
		}

		internal Task<SearchResponse> VideoSearchAsync(
			string keyword, 
			bool isTagSearch,
			uint? pageCount = null, 
			Sort? sort = null, 
			Order? order = null,
			Range? range = null,
			RankingGenre? genre = null,
			CancellationToken ct = default)
		{
			if (genre.HasValue)
            {
				ThrowExceptionIfInvalidGenre(genre.Value);
			}

			var query = new NameValueCollection()
			{
				{ "mode", "watch" },
			};

			if (pageCount is not null)
				query.Add("page", (pageCount + 1).ToString()); 

			if (sort is not null) 
				query.Add("sort", sort.Value.ToShortString());
			
			if (order is not null)
				query.Add("order", order.Value.ToChar().ToString());
            
			if (range is not null)
				query.Add("f_range", ((int)range.Value).ToString());
            
			if (genre is not null)
				query.Add("genre", genre.Value.GetDescription());

			var url = new StringBuilder(!isTagSearch ? KeywordSearchApi : TagSearchApi)
				.Append(System.Net.WebUtility.UrlEncode(keyword))
				.AppendQueryString(query)
				.ToString();

			return _Search_Internal(url, ct);
		}

		internal Task<SearchResponse> VideoSearchAsync(
			string keyword,
			bool isTagSearch,
			uint? pageCount = null,
			Sort? sort = null,
			Order? order = null,
			RangeDatePair? rangeDatePair = null,
			RankingGenre? genre = null,
			CancellationToken ct = default)
		{
			if (genre.HasValue)
			{
				ThrowExceptionIfInvalidGenre(genre.Value);
			}

			var query = new NameValueCollection()
			{
				{ "mode", "watch" },
			};

			if (pageCount is not null)
				query.Add("page", (pageCount + 1).ToString());

			if (sort is not null)
				query.Add("sort", sort.Value.ToShortString());

			if (order is not null)
				query.Add("order", order.Value.ToChar().ToString());

			if (rangeDatePair is not null)
			{
				var startDate = rangeDatePair.Value.Start;
				query.Add("start", string.Join('-', new int[] { startDate.Year, startDate.Month, startDate.Day }.Select(x => x.ToString())));
				var endDate = rangeDatePair.Value.Start;
				query.Add("end", string.Join('-', new int[] { endDate.Year, endDate.Month, endDate.Day }.Select(x => x.ToString())));
			}

			if (genre is not null)
				query.Add("genre", genre.Value.GetDescription());

			var url = new StringBuilder(!isTagSearch ? KeywordSearchApi : TagSearchApi)
				.Append(System.Net.WebUtility.UrlEncode(keyword))
				.AppendQueryString(query)
				.ToString();

			return _Search_Internal(url, ct);
		}

		private async Task<SearchResponse> _Search_Internal(string urlWithQuery, CancellationToken ct)
		{
			var res = await _context.GetAsync(urlWithQuery);
			if (!res.IsSuccessStatusCode) { return new SearchResponse() { Status = "failed" }; }

			using (var stream = await res.Content.ReadAsInputStreamAsync())
			{
				var context = AngleSharp.BrowsingContext.New();
				var document = await context.GetService<IHtmlParser>().ParseDocumentAsync(stream.AsStreamForRead(), ct);

				throw new NotImplementedException();
			}
		}
	}

	public enum Range
    {
		In1Hoour = 4,
		In24Hour = 1,
		In1Week = 2,
		InMonth = 3,
    }

	public sealed class VideoSearchQueryBuilder
    {
		internal uint? _pageCount;
		internal Sort? _sort;
		internal Order? _order;
		internal Range? _range;
		internal RangeDatePair? _rangeDatePair;
		internal RankingGenre? _genre;
        private readonly VideoSearchSubClient _videoSearchSubClient;

        internal VideoSearchQueryBuilder(VideoSearchSubClient videoSearchSubClient)
        {
            _videoSearchSubClient = videoSearchSubClient;
        }

		public Task<SearchResponse> KeywordSearchAsync(string keyword, CancellationToken ct = default)
        {
			return _rangeDatePair is not null
				? _videoSearchSubClient.VideoSearchAsync(keyword, isTagSearch: false, _pageCount, _sort, _order, _rangeDatePair, _genre, ct)
				: _videoSearchSubClient.VideoSearchAsync(keyword, isTagSearch: false, _pageCount, _sort, _order, _range, _genre, ct)
				;
		}

		public Task<SearchResponse> TagSearchAsync(string keyword, CancellationToken ct = default)
		{
			return _rangeDatePair is not null
				? _videoSearchSubClient.VideoSearchAsync(keyword, isTagSearch: true, _pageCount, _sort, _order, _rangeDatePair, _genre, ct)
				: _videoSearchSubClient.VideoSearchAsync(keyword, isTagSearch: true, _pageCount, _sort, _order, _range, _genre, ct)
				;
		}

		public VideoSearchQueryBuilder SetPageCount(uint pageCount)
        {
			_pageCount = pageCount;
			return this;
		}

		public VideoSearchQueryBuilder SetSort(Sort sort)
		{
			_sort = sort;
			return this;
		}

		public VideoSearchQueryBuilder SetOrder(Order order)
		{
			_order = order;
			return this;
		}

		public VideoSearchQueryBuilder SetRange(Range range)
		{
			_range = range;
			return this;
		}

		public VideoSearchQueryBuilder SetRange(DateTime start, DateTime end)
		{			
			_rangeDatePair = new RangeDatePair(start, end);
			return this;
		}

		public VideoSearchQueryBuilder SetGenre(RankingGenre genre)
		{
			VideoSearchSubClient.ThrowExceptionIfInvalidGenre(genre);

			_genre = genre;
			return this;
		}
	}

    public struct RangeDatePair
    {
		public RangeDatePair(DateTime start, DateTime end)
        {
			VideoSearchSubClient.ThrowExceptionIfInvalidRangeDatePair(start, end);

			Start = start;
			End = end;
        }
		public DateTime Start { get; }
		public DateTime End { get; }
	}



	public class SearchResponse
	{
		[JsonPropertyName("list")]
		public IList<ListItem> List { get; set; }
		[JsonPropertyName("count")]
		public int Count { get; set; }
		[JsonPropertyName("has_ng_video_for_adsense_on_listing")]
		public bool HasNgVideoForAdsenseOnListing { get; set; }
		[JsonPropertyName("related_tags")]
		public IList<string> RelatedTags { get; set; }
		[JsonPropertyName("page")]
		public int Page { get; set; }
		[JsonPropertyName("status")]
		public string Status { get; set; }

		public bool IsStatusOK
		{
			get { return Status == "ok"; }
		}
	}
	public class ThumbnailStyle
	{
		[JsonPropertyName("offset_x")]
		public int OffsetX { get; set; }
		[JsonPropertyName("offset_y")]
		public int OffsetY { get; set; }
		[JsonPropertyName("width")]
		public int Width { get; set; }
	}

	public class ListItem
	{
		[JsonPropertyName("id")]
		public string Id { get; set; }
		[JsonPropertyName("title")]
		public string Title { get; set; }
		[JsonPropertyName("first_retrieve")]
		public string _firstRetrieve { get; set; }
		[JsonPropertyName("view_counter")]
		public int ViewCount { get; set; }
		[JsonPropertyName("mylist_counter")]
		public int MylistCount { get; set; }
		[JsonPropertyName("thumbnail_url")]
		public string ThumbnailUrl { get; set; }
		[JsonPropertyName("num_res")]
		public int CmmentCount { get; set; }
		[JsonPropertyName("last_res_body")]
		public string LastResBody { get; set; }
		[JsonPropertyName("length")]
		public string _length { get; set; }
		[JsonPropertyName("title_short")]
		public string TitleShort { get; set; }
		[JsonPropertyName("description_short")]
		public string DescriptionShort { get; set; }
		[JsonPropertyName("is_middle_thumbnail")]
		public bool IsMiddleThumbnail { get; set; }


		private TimeSpan? __Length;
		public TimeSpan Length => __Length ??= _length.ToTimeSpan();

		private DateTime? __firstRetrieve;
		public DateTime FirstRetrieve => __firstRetrieve ??= DateTime.Parse(_firstRetrieve);

		//private void SetValuesOnDeserialized(StreamingContext context)
		//{
		//	var values = _length.Split(':').Reverse();
		//	var totalTime_Sec = 0;
		//	var q = 0;
		//	foreach (var t in values)
		//	{
		//		totalTime_Sec += int.Parse(t) * (q == 0 ? 1 : (q * 60));
		//		q++;
		//	}

		//	Length = TimeSpan.FromSeconds(totalTime_Sec);
		//}

	}

}
