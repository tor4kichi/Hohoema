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
	public class MylistPortalPageContentViewModel : PotalPageContentViewModel
	{
		public MylistPortalPageContentViewModel(PageManager pageManager, HohoemaApp hohoemaApp)
			: base(pageManager)
		{
			_HohoemaApp = hohoemaApp;

			MylistGroupItems = new ObservableCollection<MylistGroupListItem>();
		}

		protected override void NavigateTo()
		{
			try
			{
				var listItems = _HohoemaApp.UserMylistManager.UserMylists
					.Select(x => new MylistGroupListItem(x, PageManager));

				foreach (var item in listItems)
				{
					MylistGroupItems.Add(item);
				}
			}
			catch
			{
				// 個人マイリストの更新に失敗
			}


			base.NavigateTo();
		}

		private DelegateCommand _OpenMylistCommand;
		public DelegateCommand OpenMylistCommand
		{
			get
			{
				return _OpenMylistCommand
					?? (_OpenMylistCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.UserMylist);
					}));
			}
		}


		public ObservableCollection<MylistGroupListItem> MylistGroupItems { get; private set; }



		HohoemaApp _HohoemaApp;
	}
}
