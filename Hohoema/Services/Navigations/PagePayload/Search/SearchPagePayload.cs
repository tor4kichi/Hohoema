using Hohoema.Models.PageNavigation;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Hohoema.Services.Navigations;

[DataContract]
	public class SearchPagePayload : PagePayloadBase
	{
		public SearchPagePayload()
		{

		}

		public SearchPagePayload(ISearchPagePayloadContent content)
		{
			_Content = content;

			if (content is KeywordSearchPagePayloadContent)
			{
				SearchTarget = SearchTarget.Keyword;
			}
			else if (content is TagSearchPagePayloadContent)
			{
				SearchTarget = SearchTarget.Tag;
			}
			else if (content is LiveSearchPagePayloadContent)
			{
				SearchTarget = SearchTarget.Niconama;
			}

			ContentJson = content.ToParameterString();
		}

		[DataMember]
		public SearchTarget SearchTarget { get; set; }

		[DataMember]
		public string ContentJson { get; set; }

		ISearchPagePayloadContent _Content;

		public ISearchPagePayloadContent GetContentImpl()
		{
			if (_Content != null) { return _Content; }

			switch (SearchTarget)
			{
				case SearchTarget.Keyword:
					_Content = JsonSerializer.Deserialize<KeywordSearchPagePayloadContent>(ContentJson);
					break;
				case SearchTarget.Tag:
					_Content = JsonSerializer.Deserialize<TagSearchPagePayloadContent>(ContentJson);
					break;
				case SearchTarget.Niconama:
					_Content = JsonSerializer.Deserialize<LiveSearchPagePayloadContent>(ContentJson);
					break;
				default:
					break;
			}

			return _Content;
		}
	}
