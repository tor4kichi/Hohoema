using Mntone.Nico2.Searches.Community;
using Hohoema.Models.Domain;
using System.Runtime.Serialization;
using Hohoema.Models.Domain.PageNavigation;

namespace Hohoema.Models.UseCase.PageNavigation
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
