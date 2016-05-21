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
				var vm = new HisotoryVideoInfoControlViewModel(
					history.WatchCount
					, history.Title
					, history.ItemId
					, _HohoemaApp.UserSettings.NGSettings
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
		public HisotoryVideoInfoControlViewModel(uint viewCount, string title, string videoId, NGSettings ngSettings, PageManager pageManager)
			: base(title, videoId, ngSettings, null, pageManager)
		{
			UserViewCount = viewCount;
		}

		public DateTime LastWatchedAt { get; set; }
		public uint UserViewCount { get; set; }
	}
}
