using NiconicoToolkit.Live;
using Hohoema.Models.Niconico.Live;
using System;
using NiconicoToolkit;

namespace Hohoema.ViewModels.Community
{
    public class CommunityLiveInfoViewModel : ILiveContent
	{
        public CommunityLiveInfoViewModel()
        {
            //LiveInfo = info;

            //LiveId = LiveInfo.LiveId;
            //LiveTitle = LiveInfo.LiveId;
            //StartTime = LiveInfo.StartTime;
            //StreamerName = LiveInfo.StreamerName;
        }



		public LiveId LiveId { get; private set; }
		public string Title { get; private set; }
		public string StreamerName { get; private set; }
		public DateTime StartTime { get; private set; }

        public string BroadcasterId => null;

        public NiconicoId Id => LiveId;

        public string Label => Title;

    }
}
