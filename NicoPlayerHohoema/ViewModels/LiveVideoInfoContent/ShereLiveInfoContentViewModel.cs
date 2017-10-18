using NicoPlayerHohoema.Models.Live;
using NicoPlayerHohoema.Helpers;
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
		public Services.HohoemaDialogService HohoemaDialogService { get; private set; }

		public ReactiveProperty<bool> IsStillLoggedInTwitter { get; private set; }


		public ShereLiveInfoContentViewModel(NicoLiveVideo liveVideo, Services.HohoemaDialogService dialogService)
		{
			NicoLiveVideo = liveVideo;
			HohoemaDialogService = dialogService;
			IsStillLoggedInTwitter = new ReactiveProperty<bool>(!TwitterHelper.IsLoggedIn);
		}


		private DelegateCommand _ShereWithTwitterCommand;
		public DelegateCommand ShereWithTwitterCommand
		{
			get
			{
				return _ShereWithTwitterCommand
					?? (_ShereWithTwitterCommand = new DelegateCommand(async () =>
					{
						if (!TwitterHelper.IsLoggedIn)
						{
							if (!await TwitterHelper.LoginOrRefreshToken())
							{
								return;
							}
						}

						IsStillLoggedInTwitter.Value = !TwitterHelper.IsLoggedIn;

						if (TwitterHelper.IsLoggedIn)
						{
							var text = $"{NicoLiveVideo.LiveTitle} http://nico.ms/{NicoLiveVideo.LiveId} #{NicoLiveVideo.LiveId}";
							var twitterLoginUserName = TwitterHelper.TwitterUser.ScreenName;
							var customText = await HohoemaDialogService.GetTextAsync($"{twitterLoginUserName} としてTwitterへ投稿", "", text);

							if (customText != null)
							{
								var result = await TwitterHelper.SubmitTweet(customText);

								if (!result)
								{
//									_ToastNotificationService.ShowText("ツイートに失敗しました", "もう一度お試しください");
								}
							}
						}
					}
					));
			}
		}
	}
}
