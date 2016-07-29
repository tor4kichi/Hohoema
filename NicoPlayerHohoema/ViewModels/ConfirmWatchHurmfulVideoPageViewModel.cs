using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Prism.Commands;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using System.Threading;

namespace NicoPlayerHohoema.ViewModels
{
	public class ConfirmWatchHurmfulVideoPageViewModel : HohoemaViewModelBase
	{
		public ConfirmWatchHurmfulVideoPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager) 
			: base(hohoemaApp, pageManager, true)
		{
			IsNoMoreConfirmHarmfulVideo = new ReactiveProperty<bool>(false);
			Tags = new ObservableCollection<string>();
		}


		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			VideoPlayPayload payload = null;
			if (e.Parameter is string)
			{
				payload = VideoPlayPayload.FromParameterString(e.Parameter as string);
			}
			else
			{
				throw new Exception();
			}

			cancelToken.ThrowIfCancellationRequested();

			var thumbnailInfo = await NicoVideo.GetThumbnailResponse();

			VideoId = payload.VideoId;
			Quality = payload.Quality;

			NicoVideo = await HohoemaApp.MediaManager.GetNicoVideo(VideoId);

			cancelToken.ThrowIfCancellationRequested();

			SubmitDate = thumbnailInfo.PostedAt.DateTime;
			Title = NicoVideo.Title;


			Tags.Clear();
			foreach (var tag in thumbnailInfo.Tags.Value)
			{
				Tags.Add(tag.Value);
			}

			// このページに来る前のプレイヤーを忘れされる
			// ユーザーが戻るナビゲーションを行った時は、動画ページを飛ばしてさらに前のリスト系ページ等に戻る
			var dispatcher = Window.Current.CoreWindow.Dispatcher;
			await Task.Delay(100).
				ContinueWith(async prevResult =>
				{
					await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
					{
						PageManager.ForgetLastPage();
					});
				});
		}
	

		private DelegateCommand _BackCommand;
		public new DelegateCommand BackCommand
		{
			get
			{
				return _BackCommand
					?? (_BackCommand = new DelegateCommand(() =>
					{
						if (PageManager.NavigationService.CanGoBack())
						{
							PageManager.NavigationService.GoBack();
						}
						else
						{
							PageManager.OpenPage(HohoemaPageType.Portal);
						}
					}));
			}
		}


		private DelegateCommand _ContinueWatchVideoCommand;
		public DelegateCommand ContinueWatchVideoCommand
		{
			get
			{
				return _ContinueWatchVideoCommand
					?? (_ContinueWatchVideoCommand = new DelegateCommand(() =>
					{

						NicoVideo.HarmfulContentReactionType = IsNoMoreConfirmHarmfulVideo.Value ? Mntone.Nico2.HarmfulContentReactionType.ContinueWithNotMoreConfirm : Mntone.Nico2.HarmfulContentReactionType.ContinueOnce;
						PageManager.OpenPage(HohoemaPageType.VideoPlayer,
							new VideoPlayPayload()
							{
								VideoId = VideoId,
								Quality = Quality,
							}
							.ToParameterString()
						);

						PageManager.ForgetLastPage();
					}));
			}
		}


		public ReactiveProperty<bool> IsNoMoreConfirmHarmfulVideo { get; private set; }

		public NicoVideo NicoVideo { get; private set; }

		public string VideoId { get; private set; }
		public NicoVideoQuality? Quality { get; private set; }


		public DateTime SubmitDate { get; private set; }
		public string Title { get; private set; }
		public ObservableCollection<string> Tags { get; private set; }
	}
}
