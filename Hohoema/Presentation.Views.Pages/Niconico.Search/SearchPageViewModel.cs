using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hohoema.Models.Domain;
using System.Collections.ObjectModel;
using Reactive.Bindings;
using Prism.Commands;
using Prism.Mvvm;
using System.Reactive.Linq;
using System.Diagnostics;
using Reactive.Bindings.Extensions;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2.Searches.Live;
using Hohoema.Models.UseCase.PageNavigation;

using Unity;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase;
using NiconicoSession = Hohoema.Models.Domain.Niconico.NiconicoSession;
using Hohoema.Models.Domain.Niconico.Search;
using Hohoema.Models.Domain.PageNavigation;
using Prism.Navigation;
using I18NPortable;
using Hohoema.Presentation.Views.Pages.Niconico.Search;
using Hohoema.Presentation.ViewModels.Niconico.Search;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Helpers;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Search
{
    public class SearchPageViewModel : HohoemaViewModelBase, ITitleUpdatablePage, IPinablePage
    {
		public HohoemaPin GetPin()
		{
			if (_LastKeyword == null) { return null; }

			return new HohoemaPin()
			{
				Label = _LastKeyword + $" - {_LastSelectedTarget.Translate()}",
				PageType = HohoemaPageType.Search,
				Parameter = $"keyword={System.Net.WebUtility.UrlEncode(_LastKeyword)}&service={SelectedTarget.Value}",
			};
		}

		public ApplicationLayoutManager ApplicationLayoutManager { get; }
		public NiconicoSession NiconicoSession { get; }
		public SearchProvider SearchProvider { get; }
		public PageManager PageManager { get; }
		private readonly SearchHistoryRepository _searchHistoryRepository;


		public ISearchPagePayloadContent RequireSearchOption { get; private set; }


		public ReactiveCommand DoSearchCommand { get; private set; }

		public ReactiveProperty<string> SearchText { get; private set; }
		public List<SearchTarget> TargetListItems { get; private set; }
		public ReactiveProperty<SearchTarget> SelectedTarget { get; private set; }

		private static SearchTarget _LastSelectedTarget;
		private static string _LastKeyword;

		public ReactiveProperty<bool> IsNavigationFailed { get; }
		public ReactiveProperty<string> NavigationFailedReason { get; }



		public ObservableCollection<SearchHistoryListItemViewModel> SearchHistoryItems { get; private set; } = new ObservableCollection<SearchHistoryListItemViewModel>();


		private DelegateCommand _ShowSearchHistoryCommand;
		public DelegateCommand ShowSearchHistoryCommand
		{
			get
			{
				return _ShowSearchHistoryCommand
					?? (_ShowSearchHistoryCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.Search);
					}));
			}
		}


		private DelegateCommand _DeleteAllSearchHistoryCommand;
		public DelegateCommand DeleteAllSearchHistoryCommand
		{
			get
			{
				return _DeleteAllSearchHistoryCommand
					?? (_DeleteAllSearchHistoryCommand = new DelegateCommand(() =>
					{
						_searchHistoryRepository.Clear();

						SearchHistoryItems.Clear();
						RaisePropertyChanged(nameof(SearchHistoryItems));
					},
					() => _searchHistoryRepository.Count() > 0
					));
			}
		}

		private DelegateCommand<SearchHistoryListItemViewModel> _SearchHistoryItemCommand;
		public DelegateCommand<SearchHistoryListItemViewModel> SearchHistoryItemCommand
		{
			get
			{
				return _SearchHistoryItemCommand
					?? (_SearchHistoryItemCommand = new DelegateCommand<SearchHistoryListItemViewModel>((item) =>
					{
						SearchText.Value = item.Keyword;
						if (DoSearchCommand.CanExecute())
                        {
							DoSearchCommand.Execute();
						}
					}
					));
			}
		}


		private DelegateCommand<SearchHistory> _DeleteSearchHistoryItemCommand;
		public DelegateCommand<SearchHistory> DeleteSearchHistoryItemCommand
		{
			get
			{
				return _DeleteSearchHistoryItemCommand
					?? (_DeleteSearchHistoryItemCommand = new DelegateCommand<SearchHistory>((item) =>
					{
						_searchHistoryRepository.Remove(item.Keyword, item.Target);
						var itemVM = SearchHistoryItems.FirstOrDefault(x => x.Keyword == item.Keyword && x.Target == item.Target);
						if (itemVM != null)
						{
							SearchHistoryItems.Remove(itemVM);
						}
					}
					));
			}
		}


		public INavigationService NavigationService => SearchPage.ContentNavigationService;

		public SearchPageViewModel(
			ApplicationLayoutManager applicationLayoutManager,
			NiconicoSession niconicoSession,
            SearchProvider searchProvider,
            PageManager pageManager,
			SearchHistoryRepository searchHistoryRepository
            )
        {
			ApplicationLayoutManager = applicationLayoutManager;
			NiconicoSession = niconicoSession;
            SearchProvider = searchProvider;
            PageManager = pageManager;
            _searchHistoryRepository = searchHistoryRepository;
            HashSet<string> HistoryKeyword = new HashSet<string>();
            foreach (var item in _searchHistoryRepository.ReadAllItems().OrderByDescending(x => x.LastUpdated))
            {
                if (HistoryKeyword.Contains(item.Keyword))
                {
                    continue;
                }

                SearchHistoryItems.Add(new SearchHistoryListItemViewModel(item, this));
                HistoryKeyword.Add(item.Keyword);
            }

            SearchText = new ReactiveProperty<string>(_LastKeyword)
                .AddTo(_CompositeDisposable);

            TargetListItems = new List<SearchTarget>()
            {
                SearchTarget.Keyword,
                SearchTarget.Tag,
                SearchTarget.Niconama,
                SearchTarget.Mylist,
                SearchTarget.Community,
            };

            SelectedTarget = new ReactiveProperty<SearchTarget>(_LastSelectedTarget)
                .AddTo(_CompositeDisposable);

            DoSearchCommand = new ReactiveCommand()
                .AddTo(_CompositeDisposable);
#if DEBUG
			SearchText.Subscribe(x =>
            {
                Debug.WriteLine($"検索：{x}");
            });
#endif

#if DEBUG
			DoSearchCommand.CanExecuteChangedAsObservable()
                .Subscribe(x =>
                {
                    Debug.WriteLine(DoSearchCommand.CanExecute());
                });
#endif

			DoSearchCommand.Subscribe(async _ =>
            {
				await Task.Delay(50);

                if (SearchText.Value?.Length == 0) { return; }

				if (_LastSelectedTarget == SelectedTarget.Value && _LastKeyword == SearchText.Value) { return; }

				// 検索結果を表示
				PageManager.Search(SelectedTarget.Value, SearchText.Value);

                var searched = _searchHistoryRepository.Searched(SearchText.Value, SelectedTarget.Value);

                var oldSearchHistory = SearchHistoryItems.FirstOrDefault(x => x.Keyword == SearchText.Value);
                if (oldSearchHistory != null)
                {
                    SearchHistoryItems.Remove(oldSearchHistory);
                }
                SearchHistoryItems.Insert(0, new SearchHistoryListItemViewModel(searched, this));

            })
            .AddTo(_CompositeDisposable);

			IsNavigationFailed = new ReactiveProperty<bool>();
		    NavigationFailedReason = new ReactiveProperty<string>();
		}       

        public override async void OnNavigatedTo(INavigationParameters parameters)
        {
			IsNavigationFailed.Value = false;
			NavigationFailedReason.Value = null;

			try
            {
				string keyword = null;
				if (parameters.TryGetValue("keyword", out keyword))
				{
					keyword = Uri.UnescapeDataString(keyword);
				}


				SearchTarget target = SearchTarget.Keyword;
				if (!parameters.TryGetValue("service", out string modeString)
					|| !Enum.TryParse<SearchTarget>(modeString, out target)
					)
				{
					Debug.Assert(true);

					target = SearchTarget.Keyword;
				}
				
				var pageName = target switch
				{
					SearchTarget.Keyword => nameof(SearchResultKeywordPage),
					SearchTarget.Tag => nameof(SearchResultTagPage),
					SearchTarget.Niconama => nameof(SearchResultLivePage),
					SearchTarget.Mylist => nameof(SearchResultMylistPage),
					SearchTarget.Community => nameof(SearchResultCommunityPage),
					_ => null
				};

				if (pageName != null && keyword != null)
                {
					var result = await NavigationService.NavigateAsync(pageName, ("keyword", keyword));
					if (!result.Success)
					{
						throw result.Exception;
					}
				}

				SearchText.Value = keyword;
				SelectedTarget.Value = target;

				_LastSelectedTarget = target;
				_LastKeyword = keyword;
			}
			catch (Exception e)
            {
				IsNavigationFailed.Value = true;
#if DEBUG
				NavigationFailedReason.Value = e.Message;
#endif
				Debug.WriteLine(e.ToString());
			}

			base.OnNavigatedTo(parameters);
        }

        public IObservable<string> GetTitleObservable()
        {
			return SearchText.Select(x => $"{"Search".Translate()} '{x}'");
        }

        
    }
}
