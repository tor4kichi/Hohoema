using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;
using NiconicoToolkit.Video;

namespace NiconicoToolkit.SearchWithPage.Video
{
    public class SearchResponse
	{
        private readonly IHtmlDocument _document;

		public SearchResponse() { }

		public SearchResponse(IHtmlDocument document, bool isTagSearch)
        {
            _document = document;

			if (!isTagSearch)
            {
				Count = document.QuerySelector("div.message > p > span.searchTotal").TextContent.ToInt();
			}
			else
            {
				Count = document.QuerySelector("body > div.BaseLayout > section > div > div.contentBody > div.contentsData > p > span").TextContent.ToInt();
			}


			Page = document.QuerySelector("div.toolbar > div.pager > a.pagerBtn.switchingBtn.active").TextContent.ToInt();
        }


		IList<VideoSearchItem> _List;
		public IList<VideoSearchItem> List => _List ??= ParseListItems().ToList();

		IList<string> _relatedTags;
		public IList<string> RelatedTags => _relatedTags ??= ParseRelatedTags().ToList();

		public int Count { get; }
		public int Page { get; internal set; }
		public uint StatusCode { get; internal set; }

		public bool IsStatusOK
		{
			get { return StatusCode == 200; }
		}



		private IEnumerable<VideoSearchItem> ParseListItems()
        {
			var videoItemNodes = _document.QuerySelectorAll("div.contentBody.video.uad.videoList.videoList01 > ul:nth-child(2) > li");

			foreach (var node in videoItemNodes)
            {
				yield return new VideoSearchItem(node);
			}
		}


		private IEnumerable<string> ParseRelatedTags()
        {
			var tagNodes = _document.QuerySelectorAll("body > div.BaseLayout > section > div > div.tagListBox > ul > li > a");
			foreach (var node in tagNodes)
            {
				yield return node.TextContent;

			}
		}

		public class VideoSearchItem
		{
			private readonly IElement _element;

			public VideoSearchItem(IElement element)
			{
				_element = element;
			}

			private VideoId? _id;
			public VideoId Id => _id ??= _element.GetAttribute("data-video-id");

			private string _title;
			public string Title => _title ??= _element.QuerySelector(".itemTitle").TextContent;


			private int? _viewCount;
			public int ViewCount => _viewCount ??= _element.QuerySelector("div.itemContent > div.itemData > ul > li.count.view > span.value").TextContent.ToInt();

			private int? _mylistCount;
			public int MylistCount => _mylistCount ??= _element.QuerySelector("div.itemContent > div.itemData > ul > li.count.mylist > span.value").TextContent.ToInt();

			private int? _commentCount;
			public int CmmentCount => _commentCount ??= _element.QuerySelector("div.itemContent > div.itemData > ul > li.count.comment > span.value").TextContent.ToInt();

			private string _thumbnailUrl;
			public string ThumbnailUrl => _thumbnailUrl ??= _element.QuerySelector("div.videoList01Wrap > div > div.itemThumbBox > div > a > img").GetAttribute("src");

			private string _lastResBody;
			public string LastResBody => _lastResBody ??= _element.QuerySelector("div.itemContent > div.wrap > p.itemComment")?.TextContent;

			private string _descriptionShort;
			public string DescriptionShort => _descriptionShort ??= _element.QuerySelector("div.itemContent > div.wrap > p.itemDescription").FirstChild?.TextContent;

			private string _description;
			public string Description => _description ??= _element.QuerySelector("div.itemContent > div.wrap > p.itemDescription").GetAttribute("title");

			private bool? _isRequirePayment;
			public bool IsRequirePayment => _isRequirePayment ??= _element.QuerySelector(" div.videoList01Wrap > p.iconPayment") != null;

			private bool? _isAdItem;
			public bool IsAdItem => _isAdItem ??= _element.ClassList.Contains("nicoadVideoItem");

			private TimeSpan? _length;
			public TimeSpan Length => _length ??= _element.QuerySelector("span.videoLength").TextContent.ToTimeSpan();

			private DateTime? _firstRetrieve;
			public DateTime FirstRetrieve => _firstRetrieve ??= DateTime.Parse(_element.QuerySelector("div.videoList01Wrap > p > span > span.time").TextContent);
		}
	}

	

}
