using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Live;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.LiveVideoInfoContent
{
	public class SummaryLiveInfoContentViewModel : LiveInfoContentViewModelBase
	{
		public NicoLiveVideo NicoLiveVideo { get; private set; }
		public PageManager PageManager { get; private set; }


		public string LiveTitle { get; private set; }

		public string VideoTitle { get; private set; }


		public string BroadcasterName { get; private set; }

		public Uri DescriptionHtmlFileUri { get; private set; }

		public SummaryLiveInfoContentViewModel(NicoLiveVideo liveVideo, PageManager pageManager)
		{
			NicoLiveVideo = liveVideo;
			PageManager = pageManager;


		}


		public override async Task OnEnter()
		{
			DescriptionHtmlFileUri = await NicoLiveVideo.MakeLiveSummaryHtmlUri();
		}

	}
}
