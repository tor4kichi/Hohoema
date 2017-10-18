using System.Runtime.Serialization;

namespace NicoPlayerHohoema.Models
{
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
			else if (content is MylistSearchPagePayloadContent)
			{
				SearchTarget = SearchTarget.Mylist;
			}
			else if (content is CommunitySearchPagePayloadContent)
			{
				SearchTarget = SearchTarget.Community;
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
					_Content = Newtonsoft.Json.JsonConvert.DeserializeObject<KeywordSearchPagePayloadContent>(ContentJson);
					break;
				case SearchTarget.Tag:
					_Content = Newtonsoft.Json.JsonConvert.DeserializeObject<TagSearchPagePayloadContent>(ContentJson);
					break;
				case SearchTarget.Mylist:
					_Content = Newtonsoft.Json.JsonConvert.DeserializeObject<MylistSearchPagePayloadContent>(ContentJson);
					break;
				case SearchTarget.Community:
					_Content = Newtonsoft.Json.JsonConvert.DeserializeObject<CommunitySearchPagePayloadContent>(ContentJson);
					break;
				case SearchTarget.Niconama:
					_Content = Newtonsoft.Json.JsonConvert.DeserializeObject<LiveSearchPagePayloadContent>(ContentJson);
					break;
				default:
					break;
			}

			return _Content;
		}
	}
}
