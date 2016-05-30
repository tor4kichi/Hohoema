using NicoPlayerHohoema.Models;
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

			Lists = new ObservableCollection<FavoriteListViewModel>();
		}

		protected override async void NavigateTo()
		{
			while (_HohoemaApp.FavFeedManager == null)
			{
				await Task.Delay(100);
			}

			await _HohoemaApp.FavFeedManager.UpdateAll();

			Lists.Clear();

			Lists.Add(new FavoriteListViewModel()
			{
				Name = "ユーザー",
				Items = _HohoemaApp.FavFeedManager.GetFavUserFeedListAll()
					.Select(x => new FavoriteItemViewModel(x, PageManager))
					.ToList()
			});

			Lists.Add(new FavoriteListViewModel()
			{
				Name = "マイリスト",
				Items = _HohoemaApp.FavFeedManager.GetFavMylistFeedListAll()
					.Select(x => new FavoriteItemViewModel(x, PageManager))
					.ToList()
			});

			Lists.Add(new FavoriteListViewModel()
			{
				Name = "タグ",
				Items = _HohoemaApp.FavFeedManager.GetFavTagFeedListAll()
					.Select(x => new FavoriteItemViewModel(x, PageManager))
					.ToList()
			});

			base.NavigateTo();
		}


		public ObservableCollection<FavoriteListViewModel> Lists { get; private set; }


		HohoemaApp _HohoemaApp;
	}
}
