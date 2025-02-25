﻿#nullable enable
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Search;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Services;
using Hohoema.Views.Pages.Niconico.Search;
using I18NPortable;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;

namespace Hohoema.ViewModels.Pages.Niconico.Search;

public class SearchPageViewModel 
	: HohoemaPageViewModelBase
	, ITitleUpdatablePage
	, IPinablePage
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


    public IObservable<string> GetTitleObservable()
    {
        return SearchText.Select(x => $"{"Search".Translate()} '{x}'");
    }

    private readonly IMessenger _messenger;
    private readonly IScheduler _scheduler;
    private readonly SearchHistoryRepository _searchHistoryRepository;
    public ApplicationLayoutManager ApplicationLayoutManager { get; }
	public NiconicoSession NiconicoSession { get; }
	public SearchProvider SearchProvider { get; }

	public ISearchPagePayloadContent RequireSearchOption { get; private set; }
	public ReactiveCommand DoSearchCommand { get; private set; }
	public ReactiveProperty<string> SearchText { get; private set; }
	public List<SearchTarget> TargetListItems { get; private set; }
	public ReactiveProperty<SearchTarget> SelectedTarget { get; private set; }
	public ReactiveProperty<bool> IsNavigationFailed { get; }
	public ReactiveProperty<string> NavigationFailedReason { get; }



	public ObservableCollection<SearchHistoryListItemViewModel> SearchHistoryItems { get; private set; } = new ObservableCollection<SearchHistoryListItemViewModel>();


    private static SearchTarget _LastSelectedTarget;
    private static string _LastKeyword;


    public INavigationService NavigationService => SearchPage.ContentNavigationService;

	public SearchPageViewModel(
		IMessenger messenger,
		IScheduler scheduler,
		ApplicationLayoutManager applicationLayoutManager,
		NiconicoSession niconicoSession,
		SearchProvider searchProvider,		
		SearchHistoryRepository searchHistoryRepository
	)
	{
        _messenger = messenger;
        _scheduler = scheduler;
		ApplicationLayoutManager = applicationLayoutManager;
		NiconicoSession = niconicoSession;
		SearchProvider = searchProvider;		
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



		IsNavigationFailed = new ReactiveProperty<bool>();
		NavigationFailedReason = new ReactiveProperty<string>();
	}

	public override async Task OnNavigatedToAsync(INavigationParameters parameters)
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
			if (parameters.TryGetValue("service", out string modeString))
			{
				Enum.TryParse<SearchTarget>(modeString, out target);
			}
			else if (parameters.TryGetValue("service", out target))
			{

			}

			var pageName = target switch
			{
				SearchTarget.Keyword => nameof(SearchResultKeywordPage),
				SearchTarget.Tag => nameof(SearchResultTagPage),
				SearchTarget.Niconama => nameof(SearchResultLivePage),
				_ => null
			};

			if (pageName != null && keyword != null)
			{
				var result = await NavigationService.NavigateAsync(pageName, ("keyword", keyword));
				if (!result.IsSuccess)
				{
					throw result.Exception;
				}
			}

			SearchText.Value = keyword;
			SelectedTarget.Value = target;

			_LastSelectedTarget = target;
			_LastKeyword = keyword;

			DoSearchCommand.Throttle(TimeSpan.FromMilliseconds(50), _scheduler).Subscribe(_ =>
			{
				//await Task.Delay(50);

				if (SearchText.Value?.Length == 0) { return; }

				if (_LastSelectedTarget == SelectedTarget.Value && _LastKeyword == SearchText.Value) { return; }

                // 検索結果を表示
                _ = _messenger.OpenSearchPageAsync(SelectedTarget.Value, SearchText.Value);

				var searched = _searchHistoryRepository.Searched(SearchText.Value, SelectedTarget.Value);

				var oldSearchHistory = SearchHistoryItems.FirstOrDefault(x => x.Keyword == SearchText.Value);
				if (oldSearchHistory != null)
				{
					SearchHistoryItems.Remove(oldSearchHistory);
				}
				SearchHistoryItems.Insert(0, new SearchHistoryListItemViewModel(searched, this));

			})
			.AddTo(_navigationDisposables);
		}
		catch (Exception e)
		{
			IsNavigationFailed.Value = true;
#if DEBUG
				NavigationFailedReason.Value = e.Message;
#endif
			Debug.WriteLine(e.ToString());
		}

		await base.OnNavigatedToAsync(parameters);
	}

    private RelayCommand _ShowSearchHistoryCommand;
	public RelayCommand ShowSearchHistoryCommand =>
		_ShowSearchHistoryCommand ??= new RelayCommand(() =>
		{
			_ = _messenger.OpenPageAsync(HohoemaPageType.Search);
		});

    private RelayCommand _DeleteAllSearchHistoryCommand;
	public RelayCommand DeleteAllSearchHistoryCommand =>
		_DeleteAllSearchHistoryCommand ??= new RelayCommand(() =>
		{
			_searchHistoryRepository.Clear();

			SearchHistoryItems.Clear();
			OnPropertyChanged(nameof(SearchHistoryItems));
		},
		() => _searchHistoryRepository.CountSafe() > 0);

    private RelayCommand<SearchHistoryListItemViewModel> _SearchHistoryItemCommand;
	public RelayCommand<SearchHistoryListItemViewModel> SearchHistoryItemCommand =>
		_SearchHistoryItemCommand ??= new RelayCommand<SearchHistoryListItemViewModel>((item) =>
		{
			SearchText.Value = item.Keyword;
			if (DoSearchCommand.CanExecute())
			{
				DoSearchCommand.Execute();
			}
		});


    private RelayCommand<SearchHistory> _DeleteSearchHistoryItemCommand;
    public RelayCommand<SearchHistory> DeleteSearchHistoryItemCommand => 
		_DeleteSearchHistoryItemCommand ??= new RelayCommand<SearchHistory>((item) => 
		{
			_searchHistoryRepository.Remove(item.Keyword, item.Target); 
			if (SearchHistoryItems.FirstOrDefault(x => x.Keyword == item.Keyword && x.Target == item.Target) is { } itemVM) 
			{
				SearchHistoryItems.Remove(itemVM); 
			} 
		});

}
