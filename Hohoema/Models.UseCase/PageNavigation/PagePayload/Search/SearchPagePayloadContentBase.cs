using Mntone.Nico2.Live;
using Hohoema.Models.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Domain.PageNavigation;

namespace Hohoema.Models.UseCase.PageNavigation
{

    [DataContract]
	public abstract class SearchPagePayloadContentBase : PagePayloadBase, ISearchPagePayloadContent
	{
		[DataMember]
		public string Keyword { get; set; }

		public abstract SearchTarget SearchTarget { get; }
	}
}
