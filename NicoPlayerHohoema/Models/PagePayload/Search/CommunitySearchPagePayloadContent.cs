using Mntone.Nico2.Searches.Community;
using System.Runtime.Serialization;

namespace NicoPlayerHohoema.Models
{
    public class CommunitySearchPagePayloadContent : SearchPagePayloadContentBase
	{
		public override SearchTarget SearchTarget => SearchTarget.Community;


        [DataMember]
        public Mntone.Nico2.Order Order { get; set; } = Mntone.Nico2.Order.Descending;

        [DataMember]
        public CommunitySearchSort Sort { get; set; } = CommunitySearchSort.UpdateAt;

        [DataMember]
        public CommunitySearchMode Mode { get; set; } = CommunitySearchMode.Keyword;
	}
}
