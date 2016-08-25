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

			UnreadFavFeedItems = new ObservableCollection<FeedVideoInfoControlViewModel>();
		}

		protected override async void NavigateTo()
		{
			while (_HohoemaApp.FavManager == null)
			{
				await Task.Delay(100);
			}

			if (UnreadFavFeedItems.Count == 0)
			{
				await UpdateUnreadFeedItems();
			}

			base.NavigateTo();
		}		


		private async Task UpdateUnreadFeedItems()
		{
			foreach (var item in UnreadFavFeedItems)
			{
				item.Dispose();
			}

			UnreadFavFeedItems.Clear();

			await Task.Delay(0);

			// TODO: FavFeedGroupによるフィード表示に対応

			/*
			var unreadItems = _HohoemaApp.FavManager.GetAllFeedItems().Take(5);

			foreach (var item in unreadItems)
			{
				var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(item.VideoId);
				var vm = new FavoriteVideoInfoControlViewModel(item, nicoVideo, PageManager);
				UnreadFavFeedItems.Add(vm);
			}
			*/
		}

		private DelegateCommand _OpenFavFeedListCommand;
		public DelegateCommand OpenFavFeedListCommand
		{
			get
			{
				return _OpenFavFeedListCommand
					?? (_OpenFavFeedListCommand = new DelegateCommand(() =>
					{
//						PageManager.OpenPage(HohoemaPageType.FavoriteAllFeed);
					}));
			}
		}


		public ObservableCollection<FeedVideoInfoControlViewModel> UnreadFavFeedItems { get; private set; }


		HohoemaApp _HohoemaApp;
	}
}
