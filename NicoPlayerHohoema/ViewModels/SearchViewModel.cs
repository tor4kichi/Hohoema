using Mntone.Nico2.Videos.Search;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	public class SearchViewModel : BindableBase
	{
		public SearchViewModel(PageManager pageManager)
		{
			_PageManager = pageManager;

			Keyword = new ReactiveProperty<string>("");

			TargetListItems = ((IEnumerable<SearchTarget>)Enum.GetValues(typeof(SearchTarget))).ToList();

			SelectedTarget = new ReactiveProperty<SearchTarget>(TargetListItems[0]);

			#region SearchOptionListItems
			SearchOptionListItems = new List<SearchSortOptionListItem>()
			{
				new SearchSortOptionListItem()
				{
					Label = "投稿が新しい順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SearchSortMethod.FirstRetrieve,
				},
				new SearchSortOptionListItem()
				{
					Label = "投稿が古い順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SearchSortMethod.FirstRetrieve,
				},

				new SearchSortOptionListItem()
				{
					Label = "コメントが新しい順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SearchSortMethod.NewComment,
				},
				new SearchSortOptionListItem()
				{
					Label = "コメントが古い順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SearchSortMethod.NewComment,
				},

				new SearchSortOptionListItem()
				{
					Label = "再生数が多い順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SearchSortMethod.ViewCount,
				},
				new SearchSortOptionListItem()
				{
					Label = "再生数が少ない順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SearchSortMethod.ViewCount,
				},

				new SearchSortOptionListItem()
				{
					Label = "コメント数が多い順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SearchSortMethod.CommentCount,
				},
				new SearchSortOptionListItem()
				{
					Label = "コメント数が少ない順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SearchSortMethod.CommentCount,
				},


				new SearchSortOptionListItem()
				{
					Label = "再生時間が長い順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SearchSortMethod.Length,
				},
				new SearchSortOptionListItem()
				{
					Label = "再生時間が短い順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SearchSortMethod.Length,
				},

				new SearchSortOptionListItem()
				{
					Label = "マイリスト数が多い順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SearchSortMethod.MylistCount,
				},
				new SearchSortOptionListItem()
				{
					Label = "マイリスト数が少ない順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SearchSortMethod.MylistCount,
				},

				new SearchSortOptionListItem()
				{
					Label = "人気の高い順",
					SortMethod = SearchSortMethod.Popurarity,
				},
			};
			#endregion

			SelectedSearchOption = new ReactiveProperty<SearchSortOptionListItem>(SearchOptionListItems[0]);




			DoSearchCommand =
				Keyword.Select(x => !String.IsNullOrWhiteSpace(x))
				.ToReactiveCommand(false);

			DoSearchCommand.Subscribe(_ => 
			{
				if (Keyword.Value.Length == 0) { return; }

				_PageManager.OpenPage(HohoemaPageType.Search, 
					new SearchOption()
					{
						Keyword = Keyword.Value,
						SearchTarget = SelectedTarget.Value,
						SortMethod = SelectedSearchOption.Value.SortMethod,
						SortDirection = SelectedSearchOption.Value.SortDirection
					}
					.ToParameterString()
				);
			});
		}



		public ReactiveProperty<string> Keyword { get; private set; }
		public List<SearchTarget> TargetListItems { get; private set; }
		public ReactiveProperty<SearchTarget> SelectedTarget { get; private set; }
		public List<SearchSortOptionListItem> SearchOptionListItems { get; private set; }
		public ReactiveProperty<SearchSortOptionListItem> SelectedSearchOption { get; private set; }

		public ReactiveCommand DoSearchCommand { get; private set; }

		public PageManager _PageManager { get; private set; }
	}


	public class SearchSortOptionListItem
	{
		public SearchSortMethod SortMethod { get; set; }
		public Mntone.Nico2.SortDirection SortDirection { get; set; }
		public string Label { get; set; }

	}
}
