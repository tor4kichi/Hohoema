using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Util;
using NicoPlayerHohoema.Models;
using System.Diagnostics;
using Prism.Windows.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
	public class FavoriteAllFeedPageViewModel : HohoemaVideoListingPageViewModelBase<FavoriteVideoInfoControlViewModel>
	{
		public FavoriteAllFeedPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager)
		{

		}

		public override string GetPageTitle()
		{
			return "お気に入りの新着動画一覧";
		}

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);

		}


		protected override uint IncrementalLoadCount
		{
			get { return 20; }
		}

		
		protected override bool CheckNeedUpdate()
		{
			return true;
		}

		protected override IIncrementalSource<FavoriteVideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new FavriteAllFeedIncrementalSource(HohoemaApp.FavFeedManager, HohoemaApp.MediaManager, PageManager);
		}
	}


	public class FavriteAllFeedIncrementalSource : IIncrementalSource<FavoriteVideoInfoControlViewModel>
	{
		FavFeedManager _FavFeedManager;
		NiconicoMediaManager _NiconicoMediaManager;
		PageManager _PageManager;

		public List<FavFeedItem> FeedItems { get; private set; }

		public FavriteAllFeedIncrementalSource(FavFeedManager favFeedManager, NiconicoMediaManager mediaManager, PageManager pageManager)
		{
			_FavFeedManager = favFeedManager;
			_NiconicoMediaManager = mediaManager;
			_PageManager = pageManager;
		}

		public async Task<IEnumerable<FavoriteVideoInfoControlViewModel>> GetPagedItems(uint pageIndex, uint pageSize)
		{
			if (FeedItems == null)
			{
				FeedItems = _FavFeedManager.GetAllFeedItems().Take(100).ToList();
			}

			var head = pageIndex - 1;
			var currentItems = FeedItems.Skip((int)head).Take((int)pageSize).ToList();

			var list = new List<FavoriteVideoInfoControlViewModel>();
			foreach (var feed in currentItems)
			{
				try
				{
					var nicoVideo = await _NiconicoMediaManager.GetNicoVideo(feed.VideoId);
					var vm = new FavoriteVideoInfoControlViewModel(feed, nicoVideo, _PageManager);

					list.Add(vm);

					vm.LoadThumbnail();
				}
				catch (Exception ex)
				{
					Debug.Fail("FeedListのアイテムのNicoVideoの取得に失敗しました。", ex.Message);
				}
			}

			return list;
		}


		
	}
}
