using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Views.Service;
using NicoPlayerHohoema.Helpers;
using System.Windows.Input;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2;
using Prism.Windows.Navigation;
using Prism.Commands;
using Windows.UI.Xaml.Navigation;

namespace NicoPlayerHohoema.ViewModels
{

	// Note: Communityの検索はページベースで行います。
	// また、ログインが必要です。

	public class SearchResultCommunityPageViewModel : HohoemaListingPageViewModelBase<CommunityInfoControlViewModel>
	{
		public CommunitySearchPagePayloadContent SearchOption { get; private set; }

        public SearchResultCommunityPageViewModel(HohoemaApp app, PageManager pageManager)
            : base(app, pageManager, useDefaultPageTitle:false)
        {
            ChangeRequireServiceLevel(HohoemaAppServiceLevel.LoggedIn);
        }
		


		#region Commands


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

		#endregion


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            if (e.Parameter is string)
            {
                SearchOption = PagePayloadBase.FromParameterString<CommunitySearchPagePayloadContent>(e.Parameter as string);
            }

            if (SearchOption == null)
            {
                throw new Exception("コミュニティ検索");
            }


            Models.Db.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            var target = "コミュニティ";
			var optionText = Helpers.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);
			var mode = SearchOption.Mode == CommunitySearchMode.Keyword ? "キーワード" : "タグ";
			UpdateTitle($"{SearchOption.Keyword} - {target}/{optionText}({mode})");

			base.OnNavigatedTo(e, viewModelState);
		}

		protected override IIncrementalSource<CommunityInfoControlViewModel> GenerateIncrementalSource()
		{
			return new CommunitySearchSource(SearchOption, HohoemaApp, PageManager);
		}
	}

	public class CommunitySearchSource : IIncrementalSource<CommunityInfoControlViewModel>
	{
		public uint OneTimeLoadCount => 10;

		public HohoemaApp HohoemaApp { get; private set; }
		public PageManager PageManager { get; private set; }

		public uint TotalCount { get; private set; }
		public CommunitySearchResponse FirstResponse { get; private set; }

		public string SearchKeyword { get; private set; }
		public CommunitySearchMode Mode { get; private set; }
		public CommunitySearchSort Sort { get; private set; }
		public Order Order { get; private set; }

		public CommunitySearchSource(
			CommunitySearchPagePayloadContent searchOption
			, HohoemaApp app
			, PageManager pageManager
			)
		{
			HohoemaApp = app;
			PageManager = pageManager;
			SearchKeyword = searchOption.Keyword;
			Mode = searchOption.Mode;
			Sort = searchOption.Sort;
			Order = searchOption.Order;
		}

		public async Task<int> ResetSource()
		{
			try
			{
				FirstResponse = await HohoemaApp.ContentProvider.SearchCommunity(
					SearchKeyword
					, 1
					, Sort
					, Order
					, Mode
					);

				if (FirstResponse != null)
				{
					TotalCount = FirstResponse.TotalCount;
				}
			}
			catch { }
			

			return (int)TotalCount;
		}

		public async Task<IAsyncEnumerable<CommunityInfoControlViewModel>> GetPagedItems(int head, int count)
		{
			CommunitySearchResponse res = head == 0 ? FirstResponse : null;

			if (res == null)
			{
				var page = (uint)((head + count) / OneTimeLoadCount);
				res = await HohoemaApp.ContentProvider.SearchCommunity(
					SearchKeyword
					, page
					, Sort
					, Order
					, Mode
					);
			}

			if (res == null)
			{
				return AsyncEnumerable.Empty<CommunityInfoControlViewModel>();
			}

			if (false == res.IsStatusOK)
			{
				return AsyncEnumerable.Empty<CommunityInfoControlViewModel>();
			}

            return res.Communities.Select(x => new CommunityInfoControlViewModel(x, PageManager)).ToAsyncEnumerable();
		}

	}

	public class CommunityInfoControlViewModel : HohoemaListingPageItemBase, Interfaces.ICommunity
    {
		public string Name { get; private set; }
		public string ShortDescription { get; private set; }
		public string UpdateDate { get; private set; }
		public string IconUrl { get; private set; }
		public uint Level { get; private set; }
		public uint MemberCount { get; private set; }
		public uint VideoCount { get; private set; }

		public string CommunityId { get; private set; }

		public PageManager PageManager { get; private set; }

        public string Id => CommunityId;

        public CommunityInfoControlViewModel(Mntone.Nico2.Searches.Community.NicoCommynity commu, PageManager pageManager)
		{
			PageManager = pageManager;
			CommunityId = commu.Id;
            Name = commu.Name;
            ShortDescription = commu.ShortDescription;
            UpdateDate = commu.DateTime;
            IconUrl = commu.IconUrl.AbsoluteUri;

            Level = commu.Level;
			MemberCount = commu.MemberCount;
			VideoCount = commu.VideoCount;

            Label = commu.Name;
            Description = commu.ShortDescription;
            AddImageUrl(commu.IconUrl.OriginalString);
        }

        

	}
}
