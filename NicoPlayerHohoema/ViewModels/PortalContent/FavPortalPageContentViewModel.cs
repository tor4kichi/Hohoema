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
	public class FavPortalPageContentViewModel : PotalPageContentViewModel
	{
		public FavPortalPageContentViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(pageManager)
		{
			_HohoemaApp = hohoemaApp;

			UnreadFavFeedItems = new ObservableCollection<FavoriteVideoInfoControlViewModel>();
		}

		protected override async void NavigateTo()
		{
			while (_HohoemaApp.FavFeedManager == null)
			{
				await Task.Delay(100);
			}

			await UpdateUnreadFeedItems();

			base.NavigateTo();
		}		


		private async Task UpdateUnreadFeedItems()
		{
			await _HohoemaApp.FavFeedManager.UpdateAll();

			UnreadFavFeedItems.Clear();

			var unreadItems = _HohoemaApp.FavFeedManager.GetAllFeedItems().Take(10);

			foreach (var item in unreadItems)
			{
				var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(item.VideoId);
				var vm = new FavoriteVideoInfoControlViewModel(item, nicoVideo, PageManager);
				UnreadFavFeedItems.Add(vm);
			}
		}

		private DelegateCommand _OpenFavFeedListCommand;
		public DelegateCommand OpenFavFeedListCommand
		{
			get
			{
				return _OpenFavFeedListCommand
					?? (_OpenFavFeedListCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.FavoriteAllFeed);
					}));
			}
		}


		public ObservableCollection<FavoriteVideoInfoControlViewModel> UnreadFavFeedItems { get; private set; }


		HohoemaApp _HohoemaApp;
	}
}
