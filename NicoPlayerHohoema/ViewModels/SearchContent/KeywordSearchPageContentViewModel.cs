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

		public KeywordSearchPagePayloadContent SearchOption { get; private set; }
		public ReactiveProperty<int> LoadedPage { get; private set; }

		NiconicoContentFinder _ContentFinder;


		public KeywordSearchPageContentViewModel(
			KeywordSearchPagePayloadContent searchOption,
			HohoemaApp hohoemaApp, 
			PageManager pageManager, 
			MylistRegistrationDialogService mylistDialogService
			) 
			: base(hohoemaApp, pageManager, mylistDialogService)
		{
			_ContentFinder = HohoemaApp.ContentFinder;

			FailLoading = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);

			LoadedPage = new ReactiveProperty<int>(1)
				.AddTo(_CompositeDisposable);

			SearchOption = searchOption;
		}


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			var target = "キーワード";
			var optionText = Util.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);
			UpdateTitle($"{SearchOption.Keyword} - {target}/{optionText}");

			base.OnNavigatedTo(e, viewModelState);
		}

		#region Implement HohoemaVideListViewModelBase



		protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new VideoSearchSource(SearchOption, HohoemaApp, PageManager);
		}

		protected override void PostResetList()
		{
		}
		

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			var source = IncrementalLoadingItems?.Source as VideoSearchSource;
			if (source == null) { return true; }

			if (SearchOption != null)
			{
				return !SearchOption.Equals(source.SearchOption);
			}
			else
			{
				return true;
			}
		}

		#endregion

	}
}
