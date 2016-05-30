using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PortalContent
{
	public class AppPortalPageContentViewModel : PotalPageContentViewModel
	{
		public AppPortalPageContentViewModel(PageManager pageManager)
			: base(pageManager)
		{
			Items = new List<AppContentItem>()
			{
				new AppContentItem("設定", AppContentType.Settings, Selected),
				new AppContentItem("このアプリについて", AppContentType.About, Selected),
				new AppContentItem("終了", AppContentType.Exit, Selected),
			};
		}


		internal void Selected(AppContentType type)
		{
			switch (type)
			{
				case AppContentType.Settings:
					PageManager.OpenPage(HohoemaPageType.Settings);
					break;
				case AppContentType.About:
//					PageManager.OpenPage(HohoemaPageType.About);
					break;
				case AppContentType.Exit:
					// TODO: ダウンロードタスクがある場合には終了の確認を行う
					App.Current.Exit();
					break;
				default:
					break;
			}
		}

		public List<AppContentItem> Items { get; private set; }
	}


	public class AppContentItem : SelectableItem<AppContentType>
	{
		public AppContentItem(string title, AppContentType type, Action<AppContentType> selectedAction)
			: base(type, selectedAction)
		{
			Title = title;
		}

		public string Title { get; private set; }
	}

	public enum AppContentType
	{
		Settings,
		About,
		Exit,
	}
}
