using Mntone.Nico2;
using Mntone.Nico2.Videos.Search;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	public class SearchViewModel : BindableBase, IDisposable
	{
		public SearchViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
		{
			_CompositeDisposable = new CompositeDisposable();
			_SearchSettings = hohoemaApp.UserSettings.SearchSettings;
			_PageManager = pageManager;
			
			Keyword = new ReactiveProperty<string>("")
				.AddTo(_CompositeDisposable);

			TargetListItems = ((IEnumerable<SearchTarget>)Enum.GetValues(typeof(SearchTarget))).ToList();

			SelectedTarget = new ReactiveProperty<SearchTarget>(TargetListItems[0])
				.AddTo(_CompositeDisposable);

			#region SearchOptionListItems
			SearchOptionListItems = new List<SearchSortOptionListItem>()
			{
				new SearchSortOptionListItem()
				{
					Label = "投稿が新しい順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SortMethod.FirstRetrieve,
				},
				new SearchSortOptionListItem()
				{
					Label = "投稿が古い順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SortMethod.FirstRetrieve,
				},

				new SearchSortOptionListItem()
				{
					Label = "コメントが新しい順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SortMethod.NewComment,
				},
				new SearchSortOptionListItem()
				{
					Label = "コメントが古い順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SortMethod.NewComment,
				},

				new SearchSortOptionListItem()
				{
					Label = "再生数が多い順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SortMethod.ViewCount,
				},
				new SearchSortOptionListItem()
				{
					Label = "再生数が少ない順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SortMethod.ViewCount,
				},

				new SearchSortOptionListItem()
				{
					Label = "コメント数が多い順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SortMethod.CommentCount,
				},
				new SearchSortOptionListItem()
				{
					Label = "コメント数が少ない順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SortMethod.CommentCount,
				},


				new SearchSortOptionListItem()
				{
					Label = "再生時間が長い順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SortMethod.Length,
				},
				new SearchSortOptionListItem()
				{
					Label = "再生時間が短い順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SortMethod.Length,
				},

				new SearchSortOptionListItem()
				{
					Label = "マイリスト数が多い順",
					SortDirection = Mntone.Nico2.SortDirection.Descending,
					SortMethod = SortMethod.MylistCount,
				},
				new SearchSortOptionListItem()
				{
					Label = "マイリスト数が少ない順",
					SortDirection = Mntone.Nico2.SortDirection.Ascending,
					SortMethod = SortMethod.MylistCount,
				},

				new SearchSortOptionListItem()
				{
					Label = "人気の高い順",
					SortMethod = SortMethod.Popurarity,
				},
			};
			#endregion

			SelectedSearchOption = new ReactiveProperty<SearchSortOptionListItem>(SearchOptionListItems[0]);




			DoSearchCommand =
				Keyword.Select(x => !String.IsNullOrEmpty(x))
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);

			DoSearchCommand.Subscribe(_ => 
			{
				if (Keyword.Value.Length == 0) { return; }

				// キーワードを検索履歴を記録
				_SearchSettings.UpdateSearchHistory(Keyword.Value);

				// 検索結果を表示
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

			HistoryKeywords = _SearchSettings.SearchHistory
				.ToReadOnlyReactiveCollection(x => new SearchHistoryKeywordItem(x, this, _SearchSettings))
				.AddTo(_CompositeDisposable);
		}


		public void Dispose()
		{
			_CompositeDisposable?.Dispose();
		}


		internal void KeywordSelected(string keyword)
		{
			Keyword.Value = keyword;
		}


		public ReadOnlyReactiveCollection<SearchHistoryKeywordItem> HistoryKeywords { get; private set; }

		public ReactiveProperty<string> Keyword { get; private set; }
		public List<SearchTarget> TargetListItems { get; private set; }
		public ReactiveProperty<SearchTarget> SelectedTarget { get; private set; }
		public List<SearchSortOptionListItem> SearchOptionListItems { get; private set; }
		public ReactiveProperty<SearchSortOptionListItem> SelectedSearchOption { get; private set; }

		public ReactiveCommand DoSearchCommand { get; private set; }

		private SearchSeetings _SearchSettings;
		public PageManager _PageManager { get; private set; }

		private CompositeDisposable _CompositeDisposable;

	}


	public class SearchSortOptionListItem
	{
		public Mntone.Nico2.SortMethod SortMethod { get; set; }
		public Mntone.Nico2.SortDirection SortDirection { get; set; }
		public string Label { get; set; }

	}


	public class SearchHistoryKeywordItem
	{
		public SearchHistoryKeywordItem(string keyword, SearchViewModel vm, SearchSeetings settings)
		{
			Keyword = keyword;
			_SearchVM = vm;
			_Settings = settings;
		}

		private DelegateCommand _SelectedKeywordCommand;
		public DelegateCommand SelectedKeywordCommand
		{
			get
			{
				return _SelectedKeywordCommand
					?? (_SelectedKeywordCommand = new DelegateCommand(() =>
					{
						_SearchVM.KeywordSelected(this.Keyword);
					}));
			}
		}

		private DelegateCommand _RemoveKeywordCommand;
		public DelegateCommand RemoveKeywordCommand
		{
			get
			{
				return _RemoveKeywordCommand
					?? (_RemoveKeywordCommand = new DelegateCommand(() => 
					{
						_Settings.RemoveSearchHistory(Keyword);
					}));
			}
		}


		public string Keyword { get; private set; }

		private SearchViewModel _SearchVM;
		private SearchSeetings _Settings;
	}
}
