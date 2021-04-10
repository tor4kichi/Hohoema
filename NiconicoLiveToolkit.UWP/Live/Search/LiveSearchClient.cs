using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.IO;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

namespace NiconicoLiveToolkit.Live.Search
{
	/// <summary>
	/// niconicoコンテンツ検索APIの生放送検索
	/// </summary>
	/// <see cref="https://site.nicovideo.jp/search-api-docs/search.html"/>
	public sealed class LiveSearchClient
    {
		internal LiveSearchClient(NiconicoContext context)
        {
            _context = context;
        }

        private readonly NiconicoContext _context;

		public async Task<LiveSearchPageScrapingResult> GetLiveSearchPageScrapingResultAsync(
			string keyword,
			LiveStatus liveStatus,
			int? pageStartWith0,
			LiveSearchPageSortOrder? sortOrder,
			string channelId,
			int? userId,
			ProviderType[] providerTypes,
			bool? isTagSearch,
			bool? disableGrouping,
			bool? timeshiftIsAvailable,
			bool? hideMemberOnly,
			CancellationToken cancellationToken
			)
		{
			StringBuilder sb = new StringBuilder("https://live.nicovideo.jp/search");

            #region Make query string to sb

            sb.Append("?keyword=");
			sb.Append(Uri.EscapeDataString(keyword));
			
			if (pageStartWith0 is not null and var pageValue)
            {
				sb.Append($"&page=");
				sb.Append(pageValue + 1);
			}

			sb.Append($"&status=");
			sb.Append(liveStatus.ToString().ToLower());

			if (sortOrder is not null and var sortOrderValue)
            {
				sb.Append($"&sortOrder=");
				sb.Append(sortOrderValue switch
                {
                    LiveSearchPageSortOrder.RecentDesc => "recentDesc",
                    LiveSearchPageSortOrder.RecentAsc => "recentAsc",
                    LiveSearchPageSortOrder.TimeshiftCountDesc => "timeshiftCountDesc",
                    LiveSearchPageSortOrder.TimeshiftCountAsc => "timeshiftCountAsc",
                    LiveSearchPageSortOrder.ViewCountDesc => "viewCountDesc",
                    LiveSearchPageSortOrder.ViewCountAsc => "viewCountAsc",
                    LiveSearchPageSortOrder.CommentCountDesc => "commentCountDesc",
                    LiveSearchPageSortOrder.CommentCountAsc => "commentCountAsc",
                    LiveSearchPageSortOrder.UserLevelDesc => "userLevelDesc",
                    LiveSearchPageSortOrder.UserLevelAsc => "userLevelAsc",
					_ => throw new NotSupportedException(),
                });
			}

			if (disableGrouping is not null and bool disableGroupingValue)
			{
				sb.Append($"&disableGrouping=");
				sb.Append(disableGroupingValue.ToString().ToLower());
			}

			if (channelId is not null and string channelIdValue)
			{
				sb.Append($"&channelId=");
				sb.Append(channelIdValue);
			}

			if (userId is not null and int userIdValue)
			{
				sb.Append($"&userId=");
				sb.Append(userIdValue);
			}

			if (providerTypes is not null)
			{
				foreach (var providerType in providerTypes)
                {
					sb.Append($"&providerTypes=");
					sb.Append(providerType switch
                    {
						ProviderType.Official => "official",
						ProviderType.Channel => "channel",
						ProviderType.Community => "community",
						_ => throw new NotSupportedException(),
                    });
				}
			}

			if (isTagSearch is not null and bool isTagSearchValue)
            {
				sb.Append($"&isTagSearch=");
				sb.Append(isTagSearchValue.ToString().ToLower());
			}

			if (timeshiftIsAvailable is not null and bool timeshiftIsAvailableValue)
			{
				sb.Append($"&timeshiftIsAvailable=");
				sb.Append(timeshiftIsAvailableValue.ToString().ToLower());
			}

			if (hideMemberOnly is not null and bool hideMemberOnlyValue)
			{
				sb.Append($"&hideMemberOnly=");
				sb.Append(hideMemberOnlyValue.ToString().ToLower());
			}

            #endregion

            string urlWithQuery = sb.ToString();
			try
			{
				var responseMessage = await _context.GetAsync(urlWithQuery, ct: cancellationToken);
				using (var inputStream = await responseMessage.Content.ReadAsInputStreamAsync())
                {
					var parser = new HtmlParser();
					var document = await parser.ParseDocumentAsync(inputStream.AsStreamForRead(), cancellationToken);

					return LiveSearchPageScrapingResult.Success(new LiveSearchPageData(document, keyword, pageStartWith0 ?? 0, liveStatus));
				}
			}
			catch (Exception e)
            {
				return LiveSearchPageScrapingResult.Failed(e);
			}
		}

		public Task<LiveSearchPageScrapingResult> GetLiveSearchPageScrapingResultAsync(LiveSearchOptionsQuery query, CancellationToken cancellationToken)
        {
			return GetLiveSearchPageScrapingResultAsync(
				query.Keyword,
				query.LiveStatus,
				query.PageStartWith0,
				query.SortOrder,
				query.ChannelId,
				query.UserId,
				query.ProviderTypes,
				query.IsTagSearch,
				query.DisableGrouping,
				query.TimeshiftIsAvailable,
				query.HideMemberOnly,
				cancellationToken
				);
		}
	}

	public sealed class LiveSearchOptionsQuery
    {
		public string Keyword { get; }
        public LiveStatus LiveStatus { get; }
        public int? PageStartWith0 { get; private set; }
		public LiveSearchPageSortOrder? SortOrder { get; private set; }
		public ProviderType[] ProviderTypes { get; private set; }
		public string ChannelId { get; private set; }
		public int? UserId { get; private set; }
		public bool? IsTagSearch { get; private set; }
		public bool? DisableGrouping { get; private set; }
		public bool? TimeshiftIsAvailable { get; private set; }
		public bool? HideMemberOnly { get; private set; }


		public static LiveSearchOptionsQuery Create(string keyword, LiveStatus liveStatus)
        {
			return new LiveSearchOptionsQuery(keyword, liveStatus);
        }

		private LiveSearchOptionsQuery(string keyword, LiveStatus liveStatus)
        {
			Keyword = keyword;
            LiveStatus = liveStatus;
        }
		
		public LiveSearchOptionsQuery UsePage(int pageStartWith0)
		{
			PageStartWith0 = pageStartWith0;
			return this;
		}

		public LiveSearchOptionsQuery UseSortOrder(LiveSearchPageSortOrder sortOrder)
        {
			SortOrder = sortOrder;
			return this;
        }

		public LiveSearchOptionsQuery UseDisableGrouping(bool disableGrouping)
		{
			DisableGrouping = disableGrouping;
			return this;
		}


		public LiveSearchOptionsQuery UseChannelId(string channelId, bool? disableGrouping = true)
		{
			if (UserId is not null) { throw new ArgumentException(); }
			ChannelId = channelId;
			DisableGrouping = disableGrouping ?? DisableGrouping;
			return this;
		}


		public LiveSearchOptionsQuery UseUserId(int userId, bool? disableGrouping = true)
		{
			if (ChannelId is not null) { throw new ArgumentException(); }
			UserId = userId;
			DisableGrouping = disableGrouping ?? DisableGrouping;
			return this;
		}

		public LiveSearchOptionsQuery UseProviderTypes(params ProviderType[] providerTypes)
        {
			ProviderTypes = providerTypes;
			return this;
		}

		public LiveSearchOptionsQuery UseProviderTypes(IEnumerable<ProviderType> providerTypes)
		{
			ProviderTypes = providerTypes.ToArray();
			return this;
		}

		public LiveSearchOptionsQuery UseIsTagSearch(bool isTagSearch)
		{
			IsTagSearch = isTagSearch;
			return this;
		}

		public LiveSearchOptionsQuery UseTimeshiftIsAvailable(bool timeshiftIsAvailable)
		{
			TimeshiftIsAvailable = timeshiftIsAvailable;
			return this;
		}

		public LiveSearchOptionsQuery UseHideMemberOnly(bool hideMemberOnly)
		{
			HideMemberOnly = hideMemberOnly;
			return this;
		}
	}


	public enum LiveSearchPageSortOrder
    {
		RecentDesc,
		RecentAsc,
		TimeshiftCountDesc,
		TimeshiftCountAsc,
		ViewCountDesc,
		ViewCountAsc,
		CommentCountDesc,
		CommentCountAsc,
		UserLevelDesc,
		UserLevelAsc,
	}


	public sealed class LiveSearchPageScrapingResult
    {
		public static LiveSearchPageScrapingResult Failed(Exception e)
        {
			return new LiveSearchPageScrapingResult(e);
		}

		private LiveSearchPageScrapingResult(Exception e)
        {
			IsSuccess = false;
			Exception = e;

		}


		public static LiveSearchPageScrapingResult Success(LiveSearchPageData data)
		{
			return new LiveSearchPageScrapingResult(data);
		}

		private LiveSearchPageScrapingResult(LiveSearchPageData data)
		{
			IsSuccess = true;
			Data = data;
		}

		public bool IsSuccess { get; }

		public LiveSearchPageData Data { get; }

		public Exception Exception { get; }
	}	


	public static class AngleSharpNodeExtensions
    {
		public static int ToIntWithQuerySelector(this IParentNode node, string selector, int @default = 0)
        {
			var selectorNode = node.QuerySelector(selector);
			var value = selectorNode.TextContent.Trim();
			return int.TryParse(value, out var val) ? val : @default;
		}

		public static string ToStringWithQuerySelector(this IParentNode node, string selector)
		{
			return node.QuerySelector(selector).TextContent.Trim();
		}
	}

	public sealed class LiveSearchPageData
    {
        private readonly IHtmlDocument _document;
        private readonly LiveStatus _targetLiveStatus;

        public string Keyword { get; }

		public int Page { get; }

		private int? _TotalCount;
		public int TotalCount => _TotalCount ??= AngleSharpNodeExtensions.ToIntWithQuerySelector(_document, "#page_cover > div.search-input-area > div > p > strong:nth-child(2)");

		public LiveSearchPageLiveContentItem[] SearchResultItems => _targetLiveStatus switch
		{
			LiveStatus.Reserved => ReservedItems,
			LiveStatus.Onair => OnAirItems,
			LiveStatus.Past => PastItems,
			_ => throw new NotSupportedException()
		};


        private LiveSearchPageLiveContentItem[] _OnAirItems;
		public LiveSearchPageLiveContentItem[] OnAirItems => _OnAirItems ??= MakeItems(_OnAirItemsElement);

		private LiveSearchPageLiveContentItem[] _ReservedItems;
		public LiveSearchPageLiveContentItem[] ReservedItems => _ReservedItems ??= MakeItems(_ReservedItemsElement);

		private LiveSearchPageLiveContentItem[] _PastItems;
		public LiveSearchPageLiveContentItem[] PastItems => _PastItems ??= MakeItems(_PastItemsElement);

		IElement _OnAirItemsElement;
		IElement _ReservedItemsElement;
		IElement _PastItemsElement;

		public LiveSearchPageData(IHtmlDocument document, string keyword, int page, LiveStatus targetLiveStatus)
        {
            _document = document;
            Keyword = keyword;
            Page = page;
            _targetLiveStatus = targetLiveStatus;
            var itemsElements = _document.GetElementsByClassName("searchPage-Layout_Section");
            switch (targetLiveStatus)
            {
                case LiveStatus.Onair:
					_OnAirItemsElement = itemsElements.ElementAtOrDefault(0);
					_PastItemsElement = itemsElements.ElementAtOrDefault(1);
					break;
				case LiveStatus.Reserved:
					_ReservedItemsElement = itemsElements.ElementAtOrDefault(0);
					_OnAirItemsElement = itemsElements.ElementAtOrDefault(1);
					_PastItemsElement = itemsElements.ElementAtOrDefault(2);
					break;
				case LiveStatus.Past:
					_PastItemsElement = itemsElements.ElementAtOrDefault(0);
					_OnAirItemsElement = itemsElements.ElementAtOrDefault(1);
					break;
            }
        }


        private LiveSearchPageLiveContentItem[] MakeItems(IElement itemsElement)
		{
			var itemList = itemsElement.GetElementsByClassName("searchPage-ProgramList_Item");
			return itemList.Select(elem => new LiveSearchPageLiveContentItem(elem)).ToArray();
		}


    }


	public enum LiveSearchItemStatus
    {
		Reserved,
		OnAir,
		PastAndPresentTimeshift,
		PastAndNotPresentTimeshift,
    }

	public sealed class LiveSearchPageLiveContentItem
	{
		private readonly IElement _element;

		public LiveSearchPageLiveContentItem(IElement element)
		{
			_element = element;
		}

		private string _liveId;
		public string LiveId => _liveId ??= _element.QuerySelector("a").GetAttribute("href").Split('/')[1];

		private LiveSearchItemStatus? _liveStatus;
		public LiveSearchItemStatus LiveStatus => _liveStatus ??= _element.QuerySelector("a > div").ClassName switch 
		{
			"searchPage-ProgramList_StatusLabel-live" => LiveSearchItemStatus.OnAir,
			"searchPage-ProgramList_StatusLabel-future" => LiveSearchItemStatus.Reserved,
			"searchPage-ProgramList_StatusLabel-timeshift" => LiveSearchItemStatus.PastAndPresentTimeshift,
			"searchPage-ProgramList_StatusLabel-close" => LiveSearchItemStatus.PastAndNotPresentTimeshift,
		};

		private Uri _thumbnail;
		public Uri Thumbnail => _thumbnail ??= new Uri(_element.QuerySelector("a > figure > img").GetAttribute("src"));

		private string _title;
		public string Title => _title ??= _element.QuerySelector("div > h1 > a").GetAttribute("title");

		private string _shortDescription;
		public string ShortDescription => _shortDescription ??= _element.QuerySelector("div > p").TextContent.Trim();


		readonly static char[] _TimeSplitter = { '/', ' ', ':', ':', '開', '始', '（', '）', '時', '間', '分' };

		private (DateTime startAt, TimeSpan duration) GetStartAtAndDuration()
		{
			var dateText = _element.QuerySelector("ul > li:nth-child(1) > span").TextContent;
			var dateTextSplit = dateText.Split(_TimeSplitter).Where(x => !string.IsNullOrWhiteSpace(x) && x.All(c => char.IsDigit(c))).Select(x => int.Parse(x)).ToArray();

			switch (LiveStatus)
			{
				case LiveSearchItemStatus.Reserved:
					// 2021/4/5 19:15 開始
					return (new DateTime(dateTextSplit[0], dateTextSplit[1], dateTextSplit[2], dateTextSplit[3], dateTextSplit[4], 0), TimeSpan.Zero);
					break;
				case LiveSearchItemStatus.OnAir:
					// 2時間14分 経過
					if (dateTextSplit.Length == 1)
					{
						var duration = new TimeSpan(0, dateTextSplit[0], 0);
						return (DateTime.Now - duration, duration);
					}
					else if (dateTextSplit.Length == 2)
					{
						var duration = new TimeSpan(dateTextSplit[0], dateTextSplit[1], 0);
						return (DateTime.Now - duration, duration);
					}
					else
					{
						// ???
					}

					break;
				case LiveSearchItemStatus.PastAndPresentTimeshift:
				case LiveSearchItemStatus.PastAndNotPresentTimeshift:
					// 2021/4/5 18:40 開始 （1時間30分）
					{
						var startAt = new DateTime(dateTextSplit[0], dateTextSplit[1], dateTextSplit[2], dateTextSplit[3], dateTextSplit[4], 0);

						if (dateTextSplit.Length == 6)
						{
							var duration = new TimeSpan(0, dateTextSplit[5], 0);
							return (startAt, duration);
						}
						else if (dateTextSplit.Length == 7)
						{
							var duration = new TimeSpan(dateTextSplit[5], dateTextSplit[6], 0);
							return (startAt, duration);
						}
						else
						{
							// ????
						}
					}

					break;
				default:
					throw new NotSupportedException();
			}

			return (DateTime.MinValue, TimeSpan.Zero);
		}


		private DateTime? _startAt;
		public DateTime StartAt
        {
			get
			{
				if (_startAt is not null) { return _startAt.Value; }

				(_startAt, _duration) = GetStartAtAndDuration();
				return _startAt.Value;
			}
        }

		
		private TimeSpan? _duration;
        public TimeSpan Duration
        {
			get
            {
				if (_duration is not null) { return _duration.Value; }

				(_startAt, _duration) = GetStartAtAndDuration();
				return _duration.Value;
			}
        }


		private ProviderType? _providerType;
		public ProviderType ProviderType
        {
			get
            {
				if (_providerType is not null) { return _providerType.Value; }

				ParseTitleIcon();
				return _providerType.Value;
			}
        }


		private void ParseTitleIcon()
        {
			var titleIconSpanItems = _element.QuerySelectorAll<IHtmlSpanElement>("div > h1 > span");
			_providerType = ProviderType.Community;
			_IsRequerePay = false;
			_isMemberOnly = false;
			foreach (var titleIconSpan in titleIconSpanItems)
            {
                switch (titleIconSpan.ClassName)
                {
					case "searchPage-ProgramList_TitleIcon-channel":
						_providerType = ProviderType.Channel;
						break;
					case "searchPage-ProgramList_TitleIcon-official":
						_providerType = ProviderType.Official;
						break;
					case "searchPage-ProgramList_TitleIcon-pay":
						_IsRequerePay = true;
						break;
					case "searchPage-ProgramList_TitleIcon-private":
						_isMemberOnly = true;
						break;
				}
			}
		}

		private string _providerId;
		public string ProviderId => _providerId ??= _element.QuerySelector<IHtmlAnchorElement>("div > div > div > p > a").Href.Split('/').Last();

		private string _providerName;
		public string ProviderName => _providerName ??= _element.QuerySelector("div > div > div > p > a").TextContent.Trim();

		private Uri _providerIcon;
		public Uri ProviderIcon => _providerIcon ??= new Uri(_element.QuerySelector("div > div > figure > a > img").GetAttribute("src"));

		private int? _visitorCount;
		public int VisitorCount => _visitorCount ??= _element.ToIntWithQuerySelector("div > ul > li:nth-child(2) > span.searchPage-ProgramList_DataText");
		private int? _commentCount;
		public int CommentCount => _commentCount ??= _element.ToIntWithQuerySelector("div > ul > li:nth-child(3) > span.searchPage-ProgramList_DataText");
		private int? _timeshiftCount;
		public int TimeshiftCount => _timeshiftCount ??= _element.ToIntWithQuerySelector("div > ul > li:nth-child(4) > span.searchPage-ProgramList_DataText");


		private bool? _IsRequerePay;
		public bool IsRequerePay
		{
			get
			{
				if (_IsRequerePay is not null) { return _IsRequerePay.Value; }

				ParseTitleIcon();
				return _IsRequerePay.Value;
			}
		}

		private bool? _isMemberOnly;
		public bool IsMemberOnly 
		{
			get
			{
				if (_isMemberOnly is not null) { return _isMemberOnly.Value; }

				ParseTitleIcon();
				return _isMemberOnly.Value;
			}
		}

		private bool? _isTimeshiftAvairable;
		public bool IsTimeshiftAvairable => _isTimeshiftAvairable ??= _element.QuerySelector("div > div > div.searchPage-ProgramList_TimeShift.timeShift") is not null;




	}

}
