using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using NicoPlayerHohoema.Models;
using System.Collections.ObjectModel;
using Mntone.Nico2.Videos.Search;
using NicoPlayerHohoema.Util;
using Reactive.Bindings;
using Prism.Commands;
using Prism.Mvvm;
using System.Reactive.Linq;

namespace NicoPlayerHohoema.ViewModels
{
	public class SearchPageViewModel : ViewModelBase
	{
		const uint MaxPagenationCount = 50;
		const int OneTimeLoadSearchItemCount = 32;

		public SearchPageViewModel(HohoemaApp hohomaApp, PageManager pageManager, NiconicoContentFinder contentFinder)
		{
			HohoemaApp = hohomaApp;
			_PageManager = pageManager;
			_ContentFinder = contentFinder;


			FailLoading = new ReactiveProperty<bool>(false);

			LoadedPage = new ReactiveProperty<int>(1);
			MaxPageCount = new ReactiveProperty<int>(1);

		}

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				SearchOption = SearchOption.FromParameterString(e.Parameter as string);
			}

			if (String.IsNullOrWhiteSpace(SearchOption.Keyword))
			{
				FailLoading.Value = true;
			}


			SearchResultItems = new IncrementalLoadingCollection<SearchPageSource, VideoInfoControlViewModel>(new SearchPageSource(this), OneTimeLoadSearchItemCount);
			OnPropertyChanged(nameof(SearchResultItems));

			base.OnNavigatedTo(e, viewModelState);
		}

		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			/*
			SearchResultItems.Clear();
			SearchResultItems = null;
			OnPropertyChanged(nameof(SearchResultItems));
			*/

			base.OnNavigatingFrom(e, viewModelState, suspending);
		}

		

		internal async Task<List<VideoInfoControlViewModel>> GetSearchPage(uint page)
		{
			var items = new List<VideoInfoControlViewModel>();

			SearchResponse response = null;
			switch (SearchOption.SearchTarget)
			{
				case SearchTarget.Keyword:
					response = await _ContentFinder.GetKeywordSearch(SearchOption.Keyword, page, SearchOption.SortMethod, SearchOption.SortDirection);
					break;
				case SearchTarget.Tag:
					response = await _ContentFinder.GetTagSearch(SearchOption.Keyword, page, SearchOption.SortMethod, SearchOption.SortDirection);
					break;
				default:
					break;
			}

			if (response == null || !response.IsStatusOK)
			{
				throw new Exception("page Load failed. " + page);
			}

			MaxPageCount.Value = Math.Min((int)Math.Floor((float)response.count / OneTimeLoadSearchItemCount), (int)MaxPagenationCount);

			foreach (var item in response.list)
			{

				var videoInfoVM = new VideoInfoControlViewModel(
							item.title
							, item.id
							, HohoemaApp.UserSettings.NGSettings
							, HohoemaApp.MediaManager
							, _PageManager
						);

				items.Add(videoInfoVM);

				videoInfoVM.LoadThumbnail();

			}

				

			return items;
		}
		

		public ReactiveProperty<bool> FailLoading { get; private set; }

		public IncrementalLoadingCollection<SearchPageSource, VideoInfoControlViewModel> SearchResultItems { get; private set; }

		public SearchOption SearchOption { get; private set; }
		public ReactiveProperty<int> LoadedPage { get; private set; }
		public ReactiveProperty<int> MaxPageCount { get; private set; }

		public ReactiveProperty<bool> NowPageLoading { get; private set; }

		NiconicoContentFinder _ContentFinder;

		public HohoemaApp HohoemaApp { get; private set; }
		PageManager _PageManager;
	}

	public class SearchPageSource : IIncrementalSource<VideoInfoControlViewModel>
	{
		public SearchPageSource(SearchPageViewModel parentVM)
		{
			_ParentVM = parentVM;
		}


		public async Task<IEnumerable<VideoInfoControlViewModel>> GetPagedItems(uint pageIndex, uint pageSize)
		{
			return await _ParentVM.GetSearchPage(pageIndex+1);
		}



		SearchPageViewModel _ParentVM;
	}

}
