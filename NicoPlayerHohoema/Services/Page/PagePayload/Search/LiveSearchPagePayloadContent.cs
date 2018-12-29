using Mntone.Nico2.Searches.Live;
using NicoPlayerHohoema.Models;
using System.Runtime.Serialization;

namespace NicoPlayerHohoema.Services.Page
{
    public class LiveSearchPagePayloadContent : SearchPagePayloadContentBase
	{
		public override SearchTarget SearchTarget => SearchTarget.Niconama;


		[DataMember]
		public bool IsTagSearch { get; set; }

		[DataMember]
		public Mntone.Nico2.Live.CommunityType? Provider { get; set; }

        [DataMember]
        public Mntone.Nico2.Order Order { get; set; } = Mntone.Nico2.Order.Ascending;

        [DataMember]
        public NicoliveSearchSort Sort { get; set; } = NicoliveSearchSort.Recent;

        [DataMember]
        public NicoliveSearchMode? Mode { get; set; } = NicoliveSearchMode.OnAir;
	}
}
