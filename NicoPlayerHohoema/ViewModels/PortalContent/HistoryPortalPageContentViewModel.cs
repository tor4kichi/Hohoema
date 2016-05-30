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

			HisotoryVideoInfoItems = new ObservableCollection<HisotoryVideoInfoControlViewModel>();

		}

		protected override async void NavigateTo()
		{
			base.NavigateTo();

			var histories = await _HohoemaApp.ContentFinder.GetHistory();


			HisotoryVideoInfoItems.Clear();

			foreach (var history in histories.Histories.Take(10))
			{
				var vm = new HisotoryVideoInfoControlViewModel(
					history.WatchCount
					, history.Title
					, history.ItemId
					, _HohoemaApp.UserSettings.NGSettings
					, PageManager
					);

				vm.LastWatchedAt = history.WatchedAt.DateTime;
				vm.MovieLength = history.Length;
				vm.ThumbnailImageUrl = history.ThumbnailUrl;

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


		public ObservableCollection<HisotoryVideoInfoControlViewModel> HisotoryVideoInfoItems { get; private set; }

		HohoemaApp _HohoemaApp;



	}
}
