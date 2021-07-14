using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NiconicoToolkit.SnapshotSearch
{
    public enum SearchSortOrder
    {
		[Description("-")]
		Desc,

		[Description("+")]
		Asc,
    }

    public record SearchSort(SearchFieldType SortKey, SearchSortOrder SortOrder)
    {
        public override string ToString()
        {
            return SortOrder.GetDescription() + SortKey.ToQueryString();
		}
    }


	public static class SearchConstants
	{
		public static readonly string VideoSearchApiUrl = "https://api.search.nicovideo.jp/api/v2/snapshot/video/contents/search";
		
		public static readonly int MaxSearchOffset = 100_000;
		public static readonly int MaxSearchLimit = 100;
		public static readonly int MaxContextLength = 40;

		public static readonly char FiledQuerySeparator = ',';

		public static readonly string QuaryParameter = "q";
		public static readonly string TargetsParameter = "targets";
		public static readonly string FieldsParameter = "fields";
		public static readonly string FiltersParameter = "filters";
		public static readonly string JsonFilterParameter = "jsonFilter";

		public static readonly string SortParameter = "_sort";
		public static readonly string OffsetParameter = "_offset";
		public static readonly string LimitParameter = "_limit";
		public static readonly string ContextParameter = "_context";
	}


	/// <summary>
	/// niconicoコンテンツ検索APIの生放送検索
	/// </summary>
	/// <see cref="https://site.nicovideo.jp/search-api-docs/search.html"/>
	public sealed class VideoSnapshotSearchClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _defaultOptions;

        internal VideoSnapshotSearchClient(NiconicoContext context, System.Text.Json.JsonSerializerOptions defaultOptions)
        {
            _context = context;
            _defaultOptions = defaultOptions;
        }

		/*
		public Task<LiveSearchResponse> GetVideoSnapshotSearchAsync(
			string q,
			SearchFieldType[] targets,
			SearchSort searchSort,
			string context,
			int? offset = null,
			int? limit = null,
			SearchFieldType[]? fields = null,
			Expression<Func<SearchFilterField, bool>> filterExpression = null
			)
		{
			return GetVideoSnapshotSearchAsync(
				q, 
				targets,
				searchSort,
				context,
				offset,
				limit,
				fields,
				searchFilter: filterExpression != null ? new ExpressionSearchFilter(filterExpression) : default(ISearchFilter)
				);
		}
		*/


		public async Task<SnapshotResponse> GetVideoSnapshotSearchAsync(
			string q,
			SearchFieldType[] targets,
			SearchSort sort,
			string context,
			int? offset = null,
			int? limit = null,
			SearchFieldType[]? fields = null,
			ISearchFilter filter = null
			)
        {
			if (offset < 0 || offset >= SearchConstants.MaxSearchOffset)
			{
				throw new ArgumentException("offset value out of bounds. (0 <= offset <= 1600)");
			}

			if (limit < 0 || limit >= SearchConstants.MaxSearchLimit)
			{
				throw new ArgumentException("limit value out of bounds. (0 <= limit <= 100)");
			}

			var dict = new NameValueCollection()
			{
				{ SearchConstants.QuaryParameter, q ?? string.Empty },
				{ SearchConstants.TargetsParameter, targets.ToQueryString() },
				{ SearchConstants.SortParameter, sort.ToString() },
				{ SearchConstants.ContextParameter, context },
			};

			if (offset is not null and int offsetValue)
            {
				dict.Add(SearchConstants.OffsetParameter, offsetValue.ToString());
			}

			if (limit is not null and int limitValue)
			{
				dict.Add(SearchConstants.LimitParameter, limitValue.ToString());
			}

			if (fields is not null)
			{
				dict.Add(SearchConstants.FieldsParameter, fields.ToQueryString());
			}

			if (filter != null)
            {
				var filters = filter.GetFilterKeyValues();
				foreach (var f in filters)
                {
					dict.Add(f.Key, f.Value);
                }
			}

			var url = new StringBuilder(SearchConstants.VideoSearchApiUrl)
				.AppendQueryString(dict)
				.ToString();

			return await _context.GetJsonAsAsync<SnapshotResponse>(url, _defaultOptions);
		}
	}
}
