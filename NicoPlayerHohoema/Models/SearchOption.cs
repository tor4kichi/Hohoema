using Mntone.Nico2;
using Mntone.Nico2.Live;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2.Searches.Live;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public abstract class PagePayloadBase
	{
		public string ToParameterString()
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(this, new Newtonsoft.Json.JsonSerializerSettings()
			{
				TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects
			});
		}

		public static T FromParameterString<T>(string json)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
		}
	}

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

	public static class SearchPagePayloadContentHelper
	{
		public static ISearchPagePayloadContent CreateDefault(SearchTarget target)
		{
			switch (target)
			{
				case SearchTarget.Keyword:
					return new KeywordSearchPagePayloadContent();
				case SearchTarget.Tag:
					return new TagSearchPagePayloadContent();
				case SearchTarget.Mylist:
					return new MylistSearchPagePayloadContent();
				case SearchTarget.Community:
					return new CommunitySearchPagePayloadContent();
				case SearchTarget.Niconama:
					return new LiveSearchPagePayloadContent();
				default:
					break;
			}

			throw new NotSupportedException();
		}
	}



	public interface ISearchPagePayloadContent
	{
		string Keyword { get; }

		string ToParameterString();
	}

	[DataContract]
	public abstract class SearchPagePayloadContent : PagePayloadBase, ISearchPagePayloadContent
	{
		[DataMember]
		public string Keyword { get; set; }

		public abstract SearchTarget SearchTarget { get; }
	}

	[DataContract]
	public abstract class VideoSearchOption : SearchPagePayloadContent, IEquatable<VideoSearchOption>
	{
		[DataMember]
		public Mntone.Nico2.Order Order { get; set; }

		[DataMember]
		public Sort Sort { get; set; }




		public override bool Equals(object obj)
		{
			if (obj is VideoSearchOption)
			{
				return Equals(obj as VideoSearchOption);
			}
			else
			{
				return base.Equals(obj);
			}
		}

		public bool Equals(VideoSearchOption other)
		{
			if (other == null) { return false; }

			return this.Keyword == other.Keyword
				&& this.SearchTarget == other.SearchTarget
				&& this.Order == other.Order
				&& this.Sort == other.Sort;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public class KeywordSearchPagePayloadContent : VideoSearchOption
	{
		public override SearchTarget SearchTarget => SearchTarget.Keyword;
	}

	public class TagSearchPagePayloadContent : VideoSearchOption
	{
		public override SearchTarget SearchTarget => SearchTarget.Tag;
	}

	public class MylistSearchPagePayloadContent : SearchPagePayloadContent
	{
		public override SearchTarget SearchTarget => SearchTarget.Mylist;

		[DataMember]
		public Mntone.Nico2.Order Order { get; set; }

		[DataMember]
		public Mntone.Nico2.Sort Sort { get; set; }
	}

	public class CommunitySearchPagePayloadContent : SearchPagePayloadContent
	{
		public override SearchTarget SearchTarget => SearchTarget.Community;


		[DataMember]
		public Mntone.Nico2.Order Order { get; set; }

		[DataMember]
		public CommunitySearchSort Sort { get; set; }

		[DataMember]
		public CommunitySearchMode Mode { get; set; }
	}


	public class LiveSearchPagePayloadContent : SearchPagePayloadContent
	{
		public override SearchTarget SearchTarget => SearchTarget.Niconama;


		[DataMember]
		public bool IsTagSearch { get; set; }

		[DataMember]
		public Mntone.Nico2.Order Order { get; set; }

		[DataMember]
		public NicoliveSearchSort Sort { get; set; }

		[DataMember]
		public NicoliveSearchMode? Mode { get; set; }
	}
}
