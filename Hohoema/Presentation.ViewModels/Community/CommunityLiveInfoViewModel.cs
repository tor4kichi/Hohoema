using Mntone.Nico2.Communities.Detail;
using NiconicoLiveToolkit.Live;
using Hohoema.Models.Domain.Niconico.Live;
using System;

namespace Hohoema.Presentation.ViewModels.Community
{
    public class CommunityLiveInfoViewModel : ILiveContent
	{
        public CommunityLiveInfoViewModel(LiveInfo info)
        {
            LiveInfo = info;

            LiveId = LiveInfo.LiveId;
            LiveTitle = LiveInfo.LiveId;
            StartTime = LiveInfo.StartTime;
            StreamerName = LiveInfo.StreamerName;
        }



        public LiveInfo LiveInfo { get; private set; }


		public string LiveId { get; private set; }
		public string LiveTitle { get; private set; }
		public string StreamerName { get; private set; }
		public DateTime StartTime { get; private set; }

        public string BroadcasterId => null;

        public string Id => LiveId;

        public string Label => LiveTitle;

        public string ProviderId => null;

        public string ProviderName => StreamerName;

        public ProviderType ProviderType => ProviderType.Official;
    }
}
