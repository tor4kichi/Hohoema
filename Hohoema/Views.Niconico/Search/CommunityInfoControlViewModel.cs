using Hohoema.Models.Domain.Niconico.Community;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Search
{
    public class CommunityInfoControlViewModel : HohoemaListingPageItemBase, ICommunity
    {
		public string Name { get; private set; }
		public string ShortDescription { get; private set; }
		public string UpdateDate { get; private set; }
		public string IconUrl { get; private set; }
		public uint Level { get; private set; }
		public uint MemberCount { get; private set; }
		public uint VideoCount { get; private set; }

		public string CommunityId { get; private set; }

        public string Id => CommunityId;

        public CommunityInfoControlViewModel(Mntone.Nico2.Searches.Community.NicoCommynity commu)
		{
			CommunityId = commu.Id;
            Name = commu.Name;
            ShortDescription = commu.ShortDescription;
            UpdateDate = commu.DateTime;
            IconUrl = commu.IconUrl.AbsoluteUri;

            Level = commu.Level;
			MemberCount = commu.MemberCount;
			VideoCount = commu.VideoCount;

            Label = commu.Name;
            Description = commu.ShortDescription;
            AddImageUrl(commu.IconUrl.OriginalString);
        }

        

	}
}
