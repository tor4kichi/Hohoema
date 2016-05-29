using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Views.Service;
using Prism.Commands;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PortalContent
{
	public class SearchPortalPageContentViewModel : PotalPageContentViewModel
	{
		// TODO: 検索履歴の表示

		public SearchPortalPageContentViewModel(PageManager pageManager, HohoemaApp hohoemaApp, ISearchDialogService searchDialog)
			: base(pageManager)
		{
			_HohoemaApp = hohoemaApp;
			_SearchDialog = searchDialog;
		}


		private DelegateCommand _SearchCommand;
		public DelegateCommand SearchCommand
		{
			get
			{
				return _SearchCommand
					?? (_SearchCommand = new DelegateCommand(() => 
					{
						_SearchDialog.ShowAsync();
					}));
			}
		}



		HohoemaApp _HohoemaApp;
		ISearchDialogService _SearchDialog;
	}
}
