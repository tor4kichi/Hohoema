using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Prism.Commands;
using NicoPlayerHohoema.Util;

namespace NicoPlayerHohoema.ViewModels
{
	public class EmptySearchPageContentViewModel : HohoemaListingPageViewModelBase<SearchHistoryListItem>
	{
		public SearchPageViewModel SearchPageViewModel { get; private set; }



		private DelegateCommand _DeleteAllSearchHistoryCommand;
		public DelegateCommand DeleteAllSearchHistoryCommand
		{
			get
			{
				return _DeleteAllSearchHistoryCommand
					?? (_DeleteAllSearchHistoryCommand = new DelegateCommand(async () => 
					{
						await HohoemaApp.UserSettings.SearchSettings.RemoveAllSearchHistory();

						await ResetList();
					},
					() => HohoemaApp.UserSettings.SearchSettings.SearchHistory.Count > 0
					));
			}
		}


		private DelegateCommand _DeleteSelectedSearchHistoryCommand;
		public DelegateCommand DeleteSelectedSearchHistoryCommand
		{
			get
			{
				return _DeleteSelectedSearchHistoryCommand
					?? (_DeleteSelectedSearchHistoryCommand = new DelegateCommand(async () =>
					{
						foreach (var item in SelectedItems)
						{
							await HohoemaApp.UserSettings.SearchSettings.RemoveSearchHistory(item.Keyword, item.Target, false);
						}

						await HohoemaApp.UserSettings.SearchSettings.Save();

						await ResetList();
					}
					, () => SelectedItems.Count > 0
					));
			}
		}

		protected override uint IncrementalLoadCount
		{
			get
			{
				return 100;
			}
		}

		protected override void PostResetList()
		{
			DeleteAllSearchHistoryCommand.RaiseCanExecuteChanged();

			base.PostResetList();
		}

		public EmptySearchPageContentViewModel(HohoemaApp hohoemaApp, PageManager pageManager, SearchPageViewModel parentPage) 
			: base(hohoemaApp, pageManager, isRequireSignIn:false)
		{
			SearchPageViewModel = parentPage;

			SelectedItems.CollectionChangedAsObservable()
				.Subscribe(_ => 
				{
					DeleteSelectedSearchHistoryCommand.RaiseCanExecuteChanged();
				});
		}

	
		protected override IIncrementalSource<SearchHistoryListItem> GenerateIncrementalSource()
		{
			return new SearchHistoryIncrementalLoadingSource(HohoemaApp, SearchPageViewModel);
		}
	}


	public class SearchHistoryIncrementalLoadingSource : IIncrementalSource<SearchHistoryListItem>
	{
		private HohoemaApp _HohoemaApp;
		private SearchPageViewModel _SearchPageViewModel;
		public SearchHistoryIncrementalLoadingSource(HohoemaApp hohoemaApp, SearchPageViewModel parentPage)
		{
			_HohoemaApp = hohoemaApp;
			_SearchPageViewModel = parentPage;
		}

		public Task<IEnumerable<SearchHistoryListItem>> GetPagedItems(uint head, uint count)
		{
			var items = _HohoemaApp.UserSettings.SearchSettings.SearchHistory.Skip((int)head - 1).Take((int)count)
				.Select(x => new SearchHistoryListItem(x, _SearchPageViewModel.OnSearchHistorySelected))
				.ToArray();

			return Task.FromResult(items.AsEnumerable());
		}

		public Task<int> ResetSource()
		{
			return Task.FromResult(_HohoemaApp.UserSettings.SearchSettings.SearchHistory.Count);
		}
	}
}
