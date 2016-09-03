using NicoPlayerHohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PortalContent
{
	public class HistoryPortalPageContentViewModel : PotalPageContentViewModel
	{
		public HistoryPortalPageContentViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(pageManager)
		{
			_HohoemaApp = hohoemaApp;

			HisotoryVideoInfoItems = new ObservableCollection<HistoryVideoInfoControlViewModel>();

		}

		protected override async Task NavigateTo()
		{
			var histories = await _HohoemaApp.ContentFinder.GetHistory();


			HisotoryVideoInfoItems.Clear();

			foreach (var history in histories.Histories.Take(5))
			{
				var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(history.Id);
				var vm = new HistoryVideoInfoControlViewModel(
					history.WatchCount
					, nicoVideo
					, PageManager
					);

				vm.LastWatchedAt = history.WatchedAt.DateTime;
				vm.MovieLength = history.Length;
				vm.ThumbnailImageUrl = history.ThumbnailUrl.AbsoluteUri;

				HisotoryVideoInfoItems.Add(vm);
			}
		}


		private DelegateCommand _OpenHistoryCommand;
		public DelegateCommand OpenHistoryCommand
		{
			get
			{
				return _OpenHistoryCommand
					?? (_OpenHistoryCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.History);
					}));
			}
		}


		public ObservableCollection<HistoryVideoInfoControlViewModel> HisotoryVideoInfoItems { get; private set; }

		HohoemaApp _HohoemaApp;



	}
}
