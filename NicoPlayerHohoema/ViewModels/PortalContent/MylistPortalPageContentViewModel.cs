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

		protected override async void NavigateTo()
		{
			try
			{
				var mylistGroups = await _HohoemaApp.ContentFinder.GetLoginUserMylistGroups();

				var torima = new Mntone.Nico2.Mylist.MylistGroupData()
				{
					Id = "0",
					Name = "とりあえずマイリスト"
				};

				mylistGroups.Insert(0, torima);

				var listItems = mylistGroups
					.Select(x => new MylistGroupListItem(x, PageManager));

				MylistGroupItems.Clear();


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
