using NicoPlayerHohoema.Models.Live;
using NicoPlayerHohoema.Util;
using Prism.Commands;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.LiveVideoInfoContent
{
	public class ShereLiveInfoContentViewModel : LiveInfoContentViewModelBase
	{
		public NicoLiveVideo NicoLiveVideo { get; private set; }
		public Views.Service.TextInputDialogService TextInputDialog { get; private set; }


		public ShereLiveInfoContentViewModel(NicoLiveVideo liveVideo, Views.Service.TextInputDialogService textInputDialog)
		{
			NicoLiveVideo = liveVideo;
			TextInputDialog = textInputDialog;
		}

	}
}
