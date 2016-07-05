using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Views.Service;
using Prism.Commands;
using Prism.Windows.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PortalContent
{
	public class SearchPortalPageContentViewModel : PotalPageContentViewModel
	{
		public SearchPortalPageContentViewModel(PageManager pageManager, HohoemaApp hohoemaApp, ISearchDialogService searchDialog)
			: base(pageManager)
		{
			_HohoemaApp = hohoemaApp;
			_SearchDialog = searchDialog;

			var searchSettings = _HohoemaApp.UserSettings.SearchSettings;

			HistoryKeywords = searchSettings.SearchHistory
				.ToReadOnlyReactiveCollection(x => 
					new PortalSearchHisotryItem(x, PageManager)
				);
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

		public ReadOnlyReactiveCollection<PortalSearchHisotryItem> HistoryKeywords { get; private set; }



		HohoemaApp _HohoemaApp;
		ISearchDialogService _SearchDialog;
	}

	public class PortalSearchHisotryItem
	{
		public PortalSearchHisotryItem(string keyword, PageManager pageManager)
		{
			_PageManager = pageManager;
			Keyword = keyword;
		}


		private DelegateCommand _SelectedKeywordCommand;
		public DelegateCommand SelectedKeywordCommand
		{
			get
			{
				return _SelectedKeywordCommand
					?? (_SelectedKeywordCommand = new DelegateCommand(() =>
					{
						_PageManager.OpenPage(HohoemaPageType.Search,
							new SearchOption()
							{
								Keyword = Keyword,
								SearchTarget = SearchTarget.Keyword,
								SortMethod = Mntone.Nico2.SortMethod.FirstRetrieve,
								SortDirection = Mntone.Nico2.SortDirection.Descending,
							}.ToParameterString());
					}));
			}
		}

		private PageManager _PageManager;
		public string Keyword { get; private set; }

	}
}
