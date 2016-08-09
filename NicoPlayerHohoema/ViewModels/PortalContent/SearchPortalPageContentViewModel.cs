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
		public SearchPortalPageContentViewModel(PageManager pageManager, HohoemaApp hohoemaApp)
			: base(pageManager)
		{
			_HohoemaApp = hohoemaApp;
		}

		private void _HohoemaApp_OnSignin()
		{
			
		}

		private DelegateCommand _SearchCommand;
		public DelegateCommand SearchCommand
		{
			get
			{
				return _SearchCommand
					?? (_SearchCommand = new DelegateCommand(() => 
					{
						PageManager.OpenPage(HohoemaPageType.Search);
					}));
			}
		}

		public ReadOnlyReactiveCollection<PortalSearchHisotryItem> HistoryKeywords { get; private set; }

		protected override void NavigateTo()
		{
			base.NavigateTo();

			var searchSettings = _HohoemaApp.UserSettings.SearchSettings;

			HistoryKeywords = searchSettings.SearchHistory
				.ToReadOnlyReactiveCollection(x =>
					new PortalSearchHisotryItem(x.Keyword, x.Target, PageManager)
				);
			OnPropertyChanged(nameof(HistoryKeywords));

		}

		HohoemaApp _HohoemaApp;
	}

	public class PortalSearchHisotryItem
	{
		public PortalSearchHisotryItem(string keyword, SearchTarget target, PageManager pageManager)
		{
			_PageManager = pageManager;
			Keyword = keyword;
			Target = target;
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
								SearchTarget = Target,
								Sort = Mntone.Nico2.Sort.FirstRetrieve,
								Order = Mntone.Nico2.Order.Descending,
							}.ToParameterString());
					}));
			}
		}

		private PageManager _PageManager;
		public string Keyword { get; private set; }
		public SearchTarget Target { get; private set; }

	}
}
