using NicoPlayerHohoema.Models;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	public class AppBarControlViewModel : BindableBase
	{
		public AppBarControlViewModel(PageManager pageManager)
		{
			_PageManager = pageManager;

			MenuItems = new List<PageTypeSelectableItem>()
			{
				new PageTypeSelectableItem(HohoemaPageType.Portal, OnMenuItemSelected, "ホーム"),
				new PageTypeSelectableItem(HohoemaPageType.RankingCategoryList, OnMenuItemSelected, "ランキング"),
				new PageTypeSelectableItem(HohoemaPageType.Favorite, OnMenuItemSelected, "お気に入り"),
				new PageTypeSelectableItem(HohoemaPageType.Mylist, OnMenuItemSelected, "マイリスト"),
				new PageTypeSelectableItem(HohoemaPageType.History, OnMenuItemSelected, "視聴履歴"),
			};
		}

		internal void OnMenuItemSelected(HohoemaPageType pageType)
		{
			_PageManager.OpenPage(pageType);
		}


		public List<PageTypeSelectableItem> MenuItems { get; private set; }

		PageManager _PageManager;
	}



	public class PageTypeSelectableItem : SelectableItem<HohoemaPageType>
	{
		public PageTypeSelectableItem(HohoemaPageType pageType, Action<HohoemaPageType> onSelected, string label)
			: base(pageType, onSelected)
		{
			Label = label;
		}


		public string Label { get; set; }
	}
}
