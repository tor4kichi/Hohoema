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


		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);

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

		protected override async Task OnNavigatedToAsync(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			await base.OnNavigatedToAsync(e, viewModelState);


			VideoPlayPayload payload = null;
			if (e.Parameter is string)
			{
				payload = VideoPlayPayload.FromParameterString(e.Parameter as string);
			}
			else
			{
				throw new Exception();
			}

			VideoId = payload.VideoId;
			Quality = payload.Quality;

			NicoVideo = await HohoemaApp.MediaManager.GetNicoVideo(VideoId);
			SubmitDate = NicoVideo.CachedThumbnailInfo.PostedAt.DateTime;
			Title = NicoVideo.Title;

			Tags.Clear();
			foreach (var tag in NicoVideo.CachedThumbnailInfo.Tags.Value)
			{
				Tags.Add(tag.Value);
			}

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
