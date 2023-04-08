#nullable enable
using Hohoema.Models.PageNavigation;
using System.Runtime.Serialization;

namespace Hohoema.Services.Navigations;


[DataContract]
	public abstract class SearchPagePayloadContentBase : PagePayloadBase, ISearchPagePayloadContent
	{
		[DataMember]
		public string Keyword { get; set; }

		public abstract SearchTarget SearchTarget { get; }
	}
