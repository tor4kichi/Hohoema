using Microsoft.Toolkit.Uwp.Services.Twitter;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Util;
using NicoPlayerHohoema.Views.Service;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.VideoInfoContent
{
	public sealed class ShereVideoInfoContentViewModel: MediaInfoViewModel
	{
		TextInputDialogService _TextInputDialogService;
		ToastNotificationService _ToastNotificationService;

		NicoVideo _NicoVideo;

		public ShereVideoInfoContentViewModel(NicoVideo nicoVideo, TextInputDialogService textInputService, ToastNotificationService toastService)
		{
			_NicoVideo = nicoVideo;
			_TextInputDialogService = textInputService;
			_ToastNotificationService = toastService;

			IsStillLoggedInTwitter = new ReactiveProperty<bool>(!TwitterHelper.IsLoggedIn)
				.AddTo(_CompositeDisposable);
		}


		public ReactiveProperty<bool> IsStillLoggedInTwitter { get; private set; }

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
							var text = $"{_NicoVideo.Title} http://nico.ms/{_NicoVideo.VideoId} #{_NicoVideo.VideoId}";
							var twitterLoginUserName = TwitterHelper.TwitterUser.ScreenName;
							var customText = await _TextInputDialogService.GetTextAsync($"{twitterLoginUserName} としてTwitterへ投稿", "", text);

							if (customText != null)
							{
								var result = await TwitterHelper.SubmitTweet(customText);

								if (!result)
								{
									_ToastNotificationService.ShowText("ツイートに失敗しました", "もう一度お試しください");
								}
							}
						}
					}
					));
			}
		}
	}
}
