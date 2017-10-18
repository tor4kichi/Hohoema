using Mntone.Nico2.Searches.Live;
using System.Runtime.Serialization;

namespace NicoPlayerHohoema.Models
{
    public class LiveSearchPagePayloadContent : SearchPagePayloadContentBase
	{
		public override SearchTarget SearchTarget => SearchTarget.Niconama;


		[DataMember]
		public bool IsTagSearch { get; set; }

		[DataMember]
		public Mntone.Nico2.Live.CommunityType? Provider { get; set; }

		[DataMember]
		public Mntone.Nico2.Order Order { get; set; }

		[DataMember]
		public NicoliveSearchSort Sort { get; set; }

		[DataMember]
		public NicoliveSearchMode? Mode { get; set; }
	}
}
