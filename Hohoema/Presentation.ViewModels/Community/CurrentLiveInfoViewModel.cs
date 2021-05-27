using Mntone.Nico2.Communities.Detail;
using NiconicoToolkit.Live;
using Hohoema.Models.Domain.Niconico.Live;

namespace Hohoema.Presentation.ViewModels.Community
{
    public class CurrentLiveInfoViewModel : ILiveContent
    {
		public string LiveTitle { get; private set; }
		public string LiveId { get; private set; }

		public CurrentLiveInfoViewModel(CommunityLiveInfo liveInfo, CommunityDetail community)
        {
			LiveTitle = liveInfo.LiveTitle;
			LiveId = liveInfo.LiveId;

            ProviderId = community.Id;
            ProviderName = community.Name;
		}

        public string Id => LiveId;

        public string Label => LiveTitle;

        public string ProviderId { get; }

        public string ProviderName { get; }

        public ProviderType ProviderType => ProviderType.Community;
    }
}
