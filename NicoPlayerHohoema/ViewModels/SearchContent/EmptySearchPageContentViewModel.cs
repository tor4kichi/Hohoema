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
using NicoPlayerHohoema.Models.Db;

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
						SearchHistoryDb.Clear();

						await ResetList();
					},
					() => SearchHistoryDb.GetHistoryCount() > 0
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
							SearchHistoryDb.RemoveHistory(item.Keyword, item.Target);
						}

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

		public Task<IEnumerable<SearchHistoryListItem>> GetPagedItems(int head, int count)
		{
			var items = SearchHistoryDb.GetHistoryItems().Skip(head).Take(count)
				.Select(x => new SearchHistoryListItem(x, _SearchPageViewModel.OnSearchHistorySelected))
				.ToArray();

			return Task.FromResult(items.AsEnumerable());
		}

		public Task<int> ResetSource()
		{
			return Task.FromResult(SearchHistoryDb.GetHistoryCount());
		}
	}
}
