using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Util;
using NicoPlayerHohoema.Views.Service;
using Windows.UI.Xaml.Navigation;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace NicoPlayerHohoema.ViewModels
{
	public class KeywordSearchPageContentViewModel : HohoemaVideoListingPageViewModelBase<VideoInfoControlViewModel>
	{

		public ReactiveProperty<bool> FailLoading { get; private set; }

		public SearchOption RequireSearchOption { get; private set; }
		public ReactiveProperty<int> LoadedPage { get; private set; }

		NiconicoContentFinder _ContentFinder;


		public KeywordSearchPageContentViewModel(HohoemaApp hohoemaApp, PageManager pageManager, MylistRegistrationDialogService mylistDialogService, SearchOption searchOption) 
			: base(hohoemaApp, pageManager, mylistDialogService)
		{
			_ContentFinder = HohoemaApp.ContentFinder;

			FailLoading = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);

			LoadedPage = new ReactiveProperty<int>(1)
				.AddTo(_CompositeDisposable);

			RequireSearchOption = searchOption;
		}

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				RequireSearchOption = SearchOption.FromParameterString(e.Parameter as string);
			}

			base.OnNavigatedTo(e, viewModelState);
		}

		#region Implement HohoemaVideListViewModelBase


		protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new VideoSearchSource(RequireSearchOption, HohoemaApp, PageManager);
		}

		protected override void PostResetList()
		{
			var source = IncrementalLoadingItems.Source as VideoSearchSource;
			var searchOption = source.SearchOption;
			var target = searchOption.SearchTarget == SearchTarget.Keyword ? "キーワード" : "タグ";
			var optionText = Util.SortHelper.ToCulturizedText(searchOption.Sort, searchOption.Order);
			UpdateTitle($"{target}検索: {searchOption.Keyword} - {optionText}");
		}

		protected override uint IncrementalLoadCount
		{
			get
			{
				return VideoSearchSource.OneTimeLoadSearchItemCount / 2;
			}
		}

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			var source = IncrementalLoadingItems.Source as VideoSearchSource;

			if (RequireSearchOption != null)
			{
				return !RequireSearchOption.Equals(source.SearchOption);
			}
			else
			{
				return true;
			}
		}

		#endregion

	}
}
