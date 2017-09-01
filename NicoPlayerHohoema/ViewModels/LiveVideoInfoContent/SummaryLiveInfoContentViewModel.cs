using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Live;
using Prism.Commands;
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

		public bool IsCommunityLive { get; private set; }

		public string CommunityName { get; private set; }

		public string BroadcasterImageUrl { get; private set; }

		public string BroadcasterName { get; private set; }

		public Uri DescriptionHtmlFileUri { get; private set; }
		public string Description { get; private set; }

		public DateTime OpenAt { get; private set; }
		public DateTime StartAt { get; private set; }
		public DateTime EndAt { get; private set; }

		public SummaryLiveInfoContentViewModel(string communityName, NicoLiveVideo liveVideo, PageManager pageManager)
		{
			NicoLiveVideo = liveVideo;
			PageManager = pageManager;

			CommunityName = communityName;

			if (liveVideo.BroadcasterCommunityId != null)
			{
				IsCommunityLive = liveVideo.BroadcasterCommunityId.StartsWith("co");
			}

			var playerStatus = NicoLiveVideo.PlayerStatusResponse;
			if (playerStatus != null)
			{
				OpenAt = playerStatus.Program.OpenedAt.DateTime;
				StartAt = playerStatus.Program.StartedAt.DateTime;
				EndAt = playerStatus.Program.EndedAt.DateTime;

				BroadcasterName = playerStatus.Program.BroadcasterName;
				BroadcasterImageUrl = playerStatus.Program.CommunityImageUrl.OriginalString;
				Description = playerStatus.Program.Description;
			}
		}


		public override async Task OnEnter()
		{
			DescriptionHtmlFileUri = await NicoLiveVideo.MakeLiveSummaryHtmlUri();
			RaisePropertyChanged(nameof(DescriptionHtmlFileUri));
		}



		private DelegateCommand _OpenBroadcasterInfoCommand;
		public DelegateCommand OpenBroadcasterInfoCommand
		{
			get
			{
				return _OpenBroadcasterInfoCommand
					?? (_OpenBroadcasterInfoCommand = new DelegateCommand(() =>
					{
						if (NicoLiveVideo.BroadcasterCommunityId == null) { return; }

						// TODO: チャンネルと公式に対応
						PageManager.OpenPage(HohoemaPageType.Community, NicoLiveVideo.BroadcasterCommunityId);
					},
					() => IsCommunityLive
					));
			}
		}

		private DelegateCommand _OpenBroadcasterUserCommand;
		public DelegateCommand OpenBroadcasterUserCommand
		{
			get
			{
				return _OpenBroadcasterUserCommand
					?? (_OpenBroadcasterUserCommand = new DelegateCommand(() =>
					{
						if (NicoLiveVideo.BroadcasterId == null) { return; }

						PageManager.OpenPage(HohoemaPageType.UserInfo, NicoLiveVideo.BroadcasterId);
					}));
			}
		}

		private DelegateCommand<Uri> _ScriptNotifyCommand;
		public DelegateCommand<Uri> ScriptNotifyCommand
		{
			get
			{
				return _ScriptNotifyCommand
					?? (_ScriptNotifyCommand = new DelegateCommand<Uri>((parameter) =>
					{
						System.Diagnostics.Debug.WriteLine($"script notified: {parameter}");

						PageManager.OpenPage(parameter);
					}));
			}
		}

	}
}
