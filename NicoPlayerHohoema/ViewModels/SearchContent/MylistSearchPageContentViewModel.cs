using Mntone.Nico2.Mylist;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NicoPlayerHohoema.Views.Service;
using Windows.UI.Xaml.Navigation;
using Mntone.Nico2.Searches.Mylist;
using Prism.Commands;

namespace NicoPlayerHohoema.ViewModels
{
	public class MylistSearchPageContentViewModel : HohoemaListingPageViewModelBase<MylistSearchListingItem>
	{
		public SearchOption SearchOption { get; private set; }

		public MylistSearchPageContentViewModel(HohoemaApp hohoemaApp, PageManager pageManager, SearchOption searchOption) 
			: base(hohoemaApp, pageManager)
		{
			SearchOption = searchOption;
		}


		#region Implement HohoemaVideListViewModelBase


		protected override IIncrementalSource<MylistSearchListingItem> GenerateIncrementalSource()
		{
			return new MylistSearchSource(SearchOption, HohoemaApp, PageManager);
		}

		protected override void PostResetList()
		{
			var source = IncrementalLoadingItems.Source as MylistSearchSource;
			var searchOption = source.SearchOption;
			var optionText = Util.SortHelper.ToCulturizedText(searchOption.Sort, searchOption.Order);
			UpdateTitle($"マイリスト検索: {searchOption.Keyword} - {optionText}");
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
			return true;
		}

		#endregion
	}

	public class MylistSearchListingItem : HohoemaListingPageItemBase
	{
		PageManager _PageManager;


		public MylistSearchListingItem(Mylistgroup mylistgroup, PageManager pageManager)
		{
			_PageManager = pageManager;

			Name = mylistgroup.Name;
			ItemCount = mylistgroup.ItemCount;
			GroupId = mylistgroup.Id;
			UpdateTime = mylistgroup.UpdateTime;

			SampleVideos = mylistgroup.SampleVideos?.ToList();
		}


		public string Name { get; private set; }
		public int ItemCount { get; private set; }
		public string GroupId { get; private set; }
		public DateTime UpdateTime { get; private set; }
		public List<Mntone.Nico2.Searches.Video.Video> SampleVideos { get; private set; }


		private DelegateCommand _OpenMylistCommand;
		public DelegateCommand OpenMylistCommand
		{
			get
			{
				return _OpenMylistCommand
					?? (_OpenMylistCommand = new DelegateCommand(() => 
					{
						_PageManager.OpenPage(HohoemaPageType.Mylist, GroupId);
					}));
			}
		}

		public override ICommand SelectedCommand
		{
			get
			{
				return OpenMylistCommand;
			}
		}

		public override void Dispose()
		{
			
		}
	}


	public class MylistSearchSource : IIncrementalSource<MylistSearchListingItem>
	{
		public const uint MaxPagenationCount = 50;
		public const int OneTimeLoadSearchItemCount = 32;

		public int MaxPageCount { get; private set; }

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
		public SearchOption SearchOption { get; private set; }


		private MylistSearchResponse _MylistGroupResponse;

		public MylistSearchSource(SearchOption searchOption, HohoemaApp hohoemaApp, PageManager pageManager)
		{
			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;
			SearchOption = searchOption;

		}

		public async Task<int> ResetSource()
		{
			_MylistGroupResponse = await _HohoemaApp.NiconicoContext.Search.MylistSearchAsync(SearchOption.Keyword, 0, 2);

			return _MylistGroupResponse.TotalCount;
		}


		

		public async Task<IEnumerable<MylistSearchListingItem>> GetPagedItems(uint head, uint count)
		{
			var items = new List<MylistSearchListingItem>();


			var response = await _HohoemaApp.NiconicoContext.Search.MylistSearchAsync(SearchOption.Keyword, head, count);


			foreach (var item in response.MylistgroupList)
			{
				// TODO: mylistGroupをリスト表示
				items.Add(new MylistSearchListingItem(item, _PageManager));
			}

			return items;
		}
	}
}
