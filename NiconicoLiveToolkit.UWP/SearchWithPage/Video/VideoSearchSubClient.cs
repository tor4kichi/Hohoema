using AngleSharp.Html.Parser;
using System;
using System.IO;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NiconicoToolkit.Ranking.Video;

namespace NiconicoToolkit.SearchWithPage.Video
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

		public const string VideoSearchPageUrl = "https://www.nicovideo.jp/search/";
		public const string TagSearchPageUrl = "https://www.nicovideo.jp/tag/";



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

			var url = new StringBuilder(!isTagSearch ? VideoSearchPageUrl : TagSearchPageUrl)
				.Append(System.Net.WebUtility.UrlEncode(keyword))
				.AppendQueryString(query)
				.ToString();

			return _Search_Internal(url, isTagSearch, ct);
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

			var url = new StringBuilder(!isTagSearch ? VideoSearchPageUrl : TagSearchPageUrl)
				.Append(System.Net.WebUtility.UrlEncode(keyword))
				.AppendQueryString(query)
				.ToString();

			return _Search_Internal(url, isTagSearch, ct);
		}

		private async Task<SearchResponse> _Search_Internal(string urlWithQuery, bool isTagSearch, CancellationToken ct)
		{
			await _context.WaitPageAccessAsync();

			var res = await _context.GetAsync(urlWithQuery);
			if (!res.IsSuccessStatusCode) { return new SearchResponse() { StatusCode = (uint)res.StatusCode }; }

			using (var stream = await res.Content.ReadAsInputStreamAsync())
			using (var context = AngleSharp.BrowsingContext.New())
			using (var document = await context.GetService<IHtmlParser>().ParseDocumentAsync(stream.AsStreamForRead(), ct))
			{
				return new SearchResponse(document, isTagSearch) { StatusCode = (uint)res.StatusCode };
			}
		}
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

		public void Clear()
        {
			_pageCount = null;
			_sort = null;
			_order = null;
			_range = null;
			_rangeDatePair = null;
			_genre = null;
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

	

}
