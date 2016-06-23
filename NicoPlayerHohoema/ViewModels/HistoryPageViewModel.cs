using NicoPlayerHohoema.Models;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using System.Collections.ObjectModel;

namespace NicoPlayerHohoema.ViewModels
{
	public class HistoryPageViewModel : ViewModelBase
	{
		public HistoryPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
		{
			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;

			HisotoryVideoInfoItems = new ObservableCollection<HisotoryVideoInfoControlViewModel>();
		}


		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			var histories = await _HohoemaApp.ContentFinder.GetHistory();


			HisotoryVideoInfoItems.Clear();

			foreach (var history in histories.Histories)
			{
				var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(history.Id);
				var vm = new HisotoryVideoInfoControlViewModel(
					history.WatchCount
					, nicoVideo
					, _PageManager
					);

				vm.LastWatchedAt = history.WatchedAt.DateTime;
				vm.MovieLength = history.Length;
				vm.ThumbnailImageUrl = history.ThumbnailUrl;

				HisotoryVideoInfoItems.Add(vm);
			}

			

			base.OnNavigatedTo(e, viewModelState);
		}

		
		public ObservableCollection<HisotoryVideoInfoControlViewModel> HisotoryVideoInfoItems { get; private set; }

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
	}


	public class HisotoryVideoInfoControlViewModel : VideoInfoControlViewModel
	{
		public HisotoryVideoInfoControlViewModel(uint viewCount, NicoVideo nicoVideo, PageManager pageManager)
			: base(nicoVideo, pageManager)
		{
			UserViewCount = viewCount;
		}

		public DateTime LastWatchedAt { get; set; }
		public uint UserViewCount { get; set; }
	}
}
