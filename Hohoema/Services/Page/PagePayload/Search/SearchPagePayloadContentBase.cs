using Mntone.Nico2.Live;
using Hohoema.Models;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services.Page
{

    [DataContract]
	public abstract class SearchPagePayloadContentBase : PagePayloadBase, ISearchPagePayloadContent
	{
		[DataMember]
		public string Keyword { get; set; }

		public abstract SearchTarget SearchTarget { get; }
	}
}
