using Hohoema.Models.Niconico.Live;
using NiconicoToolkit.Live;

namespace Hohoema.ViewModels.Community;

public class CurrentLiveInfoViewModel : ILiveContent, ILiveContentProvider
{
		public string Title { get; private set; }
		public LiveId LiveId { get; private set; }

    /*
		public CurrentLiveInfoViewModel(CommunityLiveInfo liveInfo, CommunityDetail community)
    {
			LiveTitle = liveInfo.LiveTitle;
			LiveId = liveInfo.LiveId;

        ProviderId = community.Id;
        ProviderName = community.Name;
		}
    */


    public string ProviderId { get; }

    public string ProviderName { get; }

    public ProviderType ProviderType => ProviderType.Community;
}
