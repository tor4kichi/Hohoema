using Hohoema.Models;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.PageNavigation;

namespace Hohoema.Services.Navigations
{

    [DataContract]
	public abstract class SearchPagePayloadContentBase : PagePayloadBase, ISearchPagePayloadContent
	{
		[DataMember]
		public string Keyword { get; set; }

		public abstract SearchTarget SearchTarget { get; }
	}
}
