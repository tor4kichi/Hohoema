using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Views.Service;
using NicoPlayerHohoema.Util;
using System.Windows.Input;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2;
using Prism.Windows.Navigation;

namespace NicoPlayerHohoema.ViewModels
{

	// Note: Communityの検索はページベースで行います。
	// また、ログインが必要です。

	public class CommunitySearchPageContentViewModel : HohoemaListingPageViewModelBase<CommunityInfoControlViewModel>
	{
		public CommunitySearchPagePayloadContent SearchOption { get; private set; }

		public CommunitySearchPageContentViewModel(CommunitySearchPagePayloadContent searchOption, HohoemaApp app, PageManager pageManager) 
			: base(app, pageManager, isRequireSignIn:true)
		{
			SearchOption = searchOption;
		}

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			var target = "コミュニティ";
			var optionText = Util.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);
			var mode = SearchOption.Mode == CommunitySearchMode.Keyword ? "キーワード" : "タグ";
			UpdateTitle($"{SearchOption.Keyword} - {target}/{optionText}({mode})");

			base.OnNavigatedTo(e, viewModelState);
		}

		protected override IIncrementalSource<CommunityInfoControlViewModel> GenerateIncrementalSource()
		{
			return new CommunitySearchSource(SearchOption, HohoemaApp);
		}
	}

	public class CommunitySearchSource : IIncrementalSource<CommunityInfoControlViewModel>
	{
		public uint OneTimeLoadCount => 20;

		public HohoemaApp HohoemaApp { get; private set; }

		public uint TotalCount { get; private set; }
		public CommunitySearchResponse FirstResponse { get; private set; }

		public string SearchKeyword { get; private set; }
		public CommunitySearchMode Mode { get; private set; }
		public CommunitySearchSort Sort { get; private set; }
		public Order Order { get; private set; }

		public CommunitySearchSource(
			CommunitySearchPagePayloadContent searchOption
			, HohoemaApp app
			)
		{
			HohoemaApp = app;
			SearchKeyword = searchOption.Keyword;
			Mode = searchOption.Mode;
			Sort = searchOption.Sort;
			Order = searchOption.Order;
		}

		public async Task<int> ResetSource()
		{
			try
			{
				FirstResponse = await HohoemaApp.ContentFinder.SearchCommunity(
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

		public async Task<IEnumerable<CommunityInfoControlViewModel>> GetPagedItems(int head, int count)
		{
			CommunitySearchResponse res = head == 0 ? FirstResponse : null;

			if (res == null)
			{
				var page = (uint)((head + count) / OneTimeLoadCount);
				res = await HohoemaApp.ContentFinder.SearchCommunity(
					SearchKeyword
					, page
					, Sort
					, Order
					, Mode
					);
			}

			if (res == null)
			{
				return Enumerable.Empty<CommunityInfoControlViewModel>();
			}

			if (false == res.IsStatusOK)
			{
				return Enumerable.Empty<CommunityInfoControlViewModel>();
			}

			var items = new List<CommunityInfoControlViewModel>();
			foreach (var commu in res.Communities)
			{
				var commuVM = new CommunityInfoControlViewModel(commu);
				items.Add(commuVM);
			}

			return items;
		}

	}

	public class CommunityInfoControlViewModel : HohoemaListingPageItemBase
	{
		public string Name { get; private set; }
		public string ShortDescription { get; private set; }
		public string UpdateDate { get; private set; }
		public string IconUrl { get; private set; }
		public uint Level { get; private set; }
		public uint MemberCount { get; private set; }
		public uint VideoCount { get; private set; }


		public CommunityInfoControlViewModel(Mntone.Nico2.Searches.Community.NicoCommynity commu)
		{
			Name = commu.Name;
			ShortDescription = commu.ShortDescription;
			UpdateDate = commu.DateTime;
			IconUrl = commu.IconUrl.AbsoluteUri;
			Level = commu.Level;
			MemberCount = commu.MemberCount;
			VideoCount = commu.VideoCount;
		}

		public override ICommand SelectedCommand
		{
			get
			{
				// TODO: コミュニティの概要ページ作成後に開くコマンドを作成
				throw new NotImplementedException();
			}
		}

		public override void Dispose()
		{
			
		}
	}
}
