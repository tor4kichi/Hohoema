using Mntone.Nico2.Mylist;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Helpers;
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
using Mntone.Nico2;
using Prism.Windows.Navigation;
using System.Collections.Async;

namespace NicoPlayerHohoema.ViewModels
{
	public class SearchResultMylistPageViewModel : HohoemaListingPageViewModelBase<MylistSearchListingItem>
	{
		public MylistSearchPagePayloadContent SearchOption { get; private set; }

		public SearchResultMylistPageViewModel(
			HohoemaApp hohoemaApp
			, PageManager pageManager
			) 
			: base(hohoemaApp, pageManager, useDefaultPageTitle: false)
		{
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
                SearchOption = PagePayloadBase.FromParameterString<MylistSearchPagePayloadContent>(e.Parameter as string);
            }

            if (SearchOption == null)
            {
                throw new Exception();
            }


            var target = "マイリスト";
			var optionText = Helpers.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);
			UpdateTitle($"{SearchOption.Keyword} - {target}/{optionText}");

			base.OnNavigatedTo(e, viewModelState);
		}

		#region Implement HohoemaVideListViewModelBase


		protected override IIncrementalSource<MylistSearchListingItem> GenerateIncrementalSource()
		{
			return new MylistSearchSource(SearchOption, HohoemaApp, PageManager);
		}

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			var source = IncrementalLoadingItems?.Source as MylistSearchSource;
			if (source == null) { return true; }

			if (SearchOption != null)
			{
				return !SearchOption.Equals(source.SearchOption);
			}
			else
			{
				return base.CheckNeedUpdateOnNavigateTo(mode);
			}
		}

		#endregion
	}

	public class MylistSearchListingItem : HohoemaListingPageItemBase, Interfaces.IMylist
    {
		PageManager _PageManager;

        public MylistSearchListingItem(IPlayableList playableList, PageManager pageManager)
        {
            _PageManager = pageManager;

            Name = playableList.Name;
            ItemCount = (uint)playableList.Count;
            GroupId = playableList.Id;
            
            Label = playableList.Name;

            if (playableList.ThumnailUrl != null)
            {
                AddImageUrl(playableList.ThumnailUrl);
            }
        }

        public MylistSearchListingItem(MylistGroup mylistgroup, PageManager pageManager)
		{
			_PageManager = pageManager;

			Name = mylistgroup.Name;
			Description = mylistgroup.Description;
			ItemCount = mylistgroup.ItemCount;
			GroupId = mylistgroup.Id;
			UpdateTime = mylistgroup.UpdateTime;

            Label = mylistgroup.Name;
            var thumbnails = mylistgroup.VideoInfoItems?.Select(x => x.Video.ThumbnailUrl.OriginalString);
            if (thumbnails != null)
            {
                foreach (var thumbnail in thumbnails)
                {
                    AddImageUrl(thumbnail);
                }
            }

            SampleVideos = mylistgroup.VideoInfoItems?.Select(x => x.Video).ToList() ?? new List<Mntone.Nico2.Searches.Video.Video>();
		}


		public string Name { get; set; }
		public uint ItemCount { get; set; }
		public string GroupId { get; set; }
		public DateTime UpdateTime { get; set; }
		public List<Mntone.Nico2.Searches.Video.Video> SampleVideos { get; set; }

        public string Id => GroupId;
    }


	public class MylistSearchSource : IIncrementalSource<MylistSearchListingItem>
	{
		public int MaxPageCount { get; private set; }

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
		public MylistSearchPagePayloadContent SearchOption { get; private set; }
		
		private MylistSearchResponse _MylistGroupResponse;



		public MylistSearchSource(MylistSearchPagePayloadContent searchOption, HohoemaApp hohoemaApp, PageManager pageManager)
		{
			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;
			SearchOption = searchOption;
		}





		public uint OneTimeLoadCount
		{
			get
			{
				return 10;
			}
		}


		public async Task<int> ResetSource()
		{
			// Note: 件数が1だとJsonのParseがエラーになる
			_MylistGroupResponse = await _HohoemaApp.NiconicoContext.Search.MylistSearchAsync(
				SearchOption.Keyword,
				0,
				2,
				SearchOption.Sort, 
				SearchOption.Order
				);

			return (int)_MylistGroupResponse.GetTotalCount();
		}


		

		public async Task<IAsyncEnumerable<MylistSearchListingItem>> GetPagedItems(int head, int count)
		{
			var response = await _HohoemaApp.NiconicoContext.Search.MylistSearchAsync(
				SearchOption.Keyword
				, (uint)head
				, (uint)count
				, SearchOption.Sort
				, SearchOption.Order
			);

            return response.MylistGroupItems?
                .Select(item => new MylistSearchListingItem(item, _PageManager))
                .ToAsyncEnumerable()
            ?? AsyncEnumerable.Empty<MylistSearchListingItem>();
        }
	}
}
